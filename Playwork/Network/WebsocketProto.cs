using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Playwork.Network {
    public class WebsocketMessage
    {
        public const int OP_CONTINUE = 0x0;
        public const int OP_TEXT = 0x1;
        public const int OP_BIN = 0x2;
        public const int OP_CLOSE = 0x8;
        public const int OP_PING = 0x9;
        public const int OP_PONG = 0xA;

        public bool IsFin { get; set; }
        public byte Operation { get; set; }
        public bool IsMask { get; set; }
        public int Length { get; set; }
        public byte[] Masking { get; set; }
        public string Payload { get; set; }
    }

    public class WebsocketProto : WebsocketMessage, IEncoding {
        private const int PARSE_HEAD = EncodingState.BEGIN;
        private const int PARSE_LEN = 1;
        private const int PARSE_EXT_LEN = 2;
        private const int PARSE_MASKING = 3;
        private const int PARSE_PAYLOAD = 4;
        
        public int ParseState { get; set; }
        public int ParseOffset { get; set; }
        public int ParseStatus { get; set; }

        public WebsocketProto() {
            ParseState = 0;
            ParseOffset = 0;
            ParseStatus = EncodingStatus.CONTINUE;
        }

        private byte[] GenerateHead() {
            byte[] bytes = new byte[1];

            if (IsFin)
            {
                bytes[0] += 1 << 7;
            }

            bytes[0] += Operation;

            return bytes;
        }

        private byte[] GenerateLen() {
            byte[] bytes = new byte[1];

            if (IsMask)
            {
                bytes[0] += 1 << 7;
            }

            if (Payload.Length < 126)
            {
                bytes[0] += (byte) Payload.Length;
            }
            else if (Payload.Length < 65536)
            {
                bytes[0] += 126;
            }
            else
            {
                bytes[0] += 127;
            }

            return bytes;
        }

        private byte[] GenerateShortExtendedLength() {
            byte[] bytes = new byte[2];
            UInt16 shortLength = (ushort) Length;

            Buffer.BlockCopy(BitConverter.GetBytes(shortLength), 0, bytes, 0, 2);
            Array.Reverse(bytes);

            return bytes;
        }

        private byte[] GenerateLongExtendedLength() {
            byte[] bytes = new byte[8];
            UInt64 longLength = (ulong) Length;

            Buffer.BlockCopy(BitConverter.GetBytes(longLength), 0, bytes, 0, 8);
            Array.Reverse(bytes);

            return bytes;
        }

        private byte[] GenerateExtendedLength() {
            if (Length < 65536)
            {
                return GenerateShortExtendedLength();
            }

            return GenerateLongExtendedLength();
        }

        private byte[] GenerateMasking() {
            return Masking;
        }
        
        private byte[] GeneratePayload() {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Payload);
            
            if (IsMask)
            {
                byte[] encoded = new byte[bytes.Length];

                for (int i = 0; i < bytes.Length; i++)
                {
                    encoded[i] = (Byte)(bytes[i] ^ Masking[i % 4]);
                }

                return encoded;
            }

            return bytes;
        }

        private int ParseHead(byte[] bytes) {
            int offset = ParseOffset;

            if (bytes.Length >= 1 + offset)
            {
                byte head = bytes[offset];
                byte fin = 1 << 7;

                if ((head & fin) > 0)
                {
                    IsFin = true;
                    head &= (byte) ~fin;
                }

                Operation = head;

                ParseOffset += 1;
                ParseState = PARSE_LEN;
                return EncodingStatus.CONTINUE;
            }

            return EncodingStatus.PENDING;
        }

        private int ParseLen(byte[] bytes)
        {
            int offset = ParseOffset;

            if (bytes.Length >= 1 + offset)
            {
                byte len = bytes[offset];
                byte mask = 1 << 7;

                if ((len & mask) > 0)
                {
                    IsMask = true;
                    len &= (byte) ~mask;
                }

                Length = len;
                ParseOffset += 1;

                if (len < 126)
                {
                    if (IsMask) {
                        ParseState = PARSE_MASKING;
                        return EncodingStatus.CONTINUE;
                    }
                    
                    ParseState = PARSE_PAYLOAD;
                    return EncodingStatus.CONTINUE;
                }
                
                ParseState = PARSE_EXT_LEN;
                return EncodingStatus.CONTINUE;
            }

            return EncodingStatus.PENDING;
        }

        private int ParseShortExtendedLength(byte[] bytes)
        {
            int offset = ParseOffset;

            if (bytes.Length >= 2 + offset) {
                byte[] shortLength = new byte[2];

                Buffer.BlockCopy(bytes, offset, shortLength, 0, 2);

                Array.Reverse(shortLength);

                Length = BitConverter.ToUInt16(shortLength, 0);

                ParseOffset += 2;

                if (IsMask) {
                    ParseState = PARSE_MASKING;
                    return EncodingStatus.CONTINUE;
                }

                ParseState = PARSE_PAYLOAD;
                return EncodingStatus.CONTINUE;
            }

            return EncodingStatus.PENDING;
        }

        private int ParseLongExtendedLength(byte[] bytes)
        {
            int offset = ParseOffset;

            if (bytes.Length >= 8 + offset) {
                byte[] shortLength = new byte[8];

                Buffer.BlockCopy(bytes, offset, shortLength, 0, 8);

                Array.Reverse(shortLength);

                Length = BitConverter.ToUInt16(shortLength, 0);

                ParseOffset += 8;

                if (IsMask) {
                    ParseState = PARSE_MASKING;
                    return EncodingStatus.CONTINUE;
                }

                ParseState = PARSE_PAYLOAD;
                return EncodingStatus.CONTINUE;
            }

            return EncodingStatus.PENDING;
        }

        private int ParseExtendedLength(byte[] bytes)
        {
            if (Length == 126)
            {
                return ParseShortExtendedLength(bytes);
            }

            return ParseLongExtendedLength(bytes);
        }

        private int ParseMasking(byte[] bytes)
        {
            if (!IsMask)
            {
                return 0;
            }

            int offset = ParseOffset;

            if (bytes.Length >= 4 + offset)
            {
                byte[] masking = new byte[4];
                Buffer.BlockCopy(bytes, offset, masking, 0, 4);
                Masking = masking;

                ParseOffset += 4;
                ParseState = PARSE_PAYLOAD;
                return EncodingStatus.CONTINUE;
            }

            return EncodingStatus.PENDING;
        }

        private int ParsePayload(byte[] bytes)
        {
            int offset = ParseOffset;

            if (bytes.Length >= Length + offset)
            {
                byte[] payload = new byte[Length];
                Buffer.BlockCopy(bytes, offset, payload, 0, Length);

                if (IsMask)
                {
                    byte[] decoded = new byte[payload.Length];

                    for (int i = 0; i < payload.Length; i++)
                    {
                        decoded[i] = (Byte)(payload[i] ^ Masking[i % 4]);
                    }

                    Payload = System.Text.Encoding.UTF8.GetString(decoded);

                    ParseOffset += Length;
                    ParseState = PARSE_PAYLOAD;
                    return EncodingStatus.COMPLETE;
                }

                Payload = System.Text.Encoding.UTF8.GetString(payload);

                ParseOffset += Length;
                ParseState = PARSE_PAYLOAD;
                return EncodingStatus.COMPLETE;
            }

            return EncodingStatus.PENDING;
        }

        public byte[] Generate()
        {
            byte[] bytes;
            List<byte[]> byteList = new List<byte[]>();

            byteList.Add(GenerateHead());

            byteList.Add(GenerateLen());

            if (Length > 125)
            {
                byteList.Add(GenerateExtendedLength());
            }

            if (IsMask)
            {
                byteList.Add(GenerateMasking());
            }

            byteList.Add(GeneratePayload());

            int totalSize = 0;

            foreach (byte[] li in byteList) {
                totalSize += li.Count();
            }

            bytes = new byte[totalSize];

            int offset = 0;

            foreach (byte[] li in byteList) {
                System.Buffer.BlockCopy(li, 0, bytes, offset, li.Count());
                offset += li.Count();
            }

            return bytes;
        }

        public int Parse(byte[] bytes)
        {
            int status = 0;

            switch(ParseState)
            {
                case PARSE_HEAD:
                    status =  ParseHead(bytes);
                    break;

                case PARSE_LEN:
                    status =  ParseLen(bytes);
                    break;

                case PARSE_EXT_LEN:
                    status = ParseExtendedLength(bytes);
                    break;

                case PARSE_MASKING:
                    status = ParseMasking(bytes);
                    break;

                case PARSE_PAYLOAD:
                    status = ParsePayload(bytes);
                    break;
            }

            return status;
        }

        public void RefreshMasking() {
            byte[] bytes = new byte[4];
            Random rand = new Random();

            for (int i = 0; i < 4; ++i) {
                bytes[i] = (byte)rand.Next(1, 255);
            }

            Masking = bytes;
        }
    }
}
