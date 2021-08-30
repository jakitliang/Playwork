using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Playwork.Network {
    public struct SimpleMessage {
        public Byte Type;
        public Int32 Size;
        public string Entity;
    }

    public class SimpleProto : SimpleMessage {
        Codec<SimpleMessage> codec;

        public SimpleProto() {
            this.codec = 
        }

        public class ParseState {
            public const int TYPE = 0;
            public const int SIZE = 1;
            public const int ENTITY = 2;
        }

        public SimpleProto() {
            this.ParseOffset = 0;
            this.parseState = ParseState.TYPE;
        }

        protected override byte[] Generate(SimpleMessage t) {
            int size = sizeof(Int32) * 2 + t.Entity.Length;
            IntPtr data = Marshal.AllocHGlobal(size);

            Marshal.WriteByte(data, t.Type);
            Marshal.WriteInt32(data, t.Size);
            Marshal.WriteIntPtr(data, Marshal.StringToHGlobalAnsi(t.Entity));

            byte[] buffer = new byte[size];

            Marshal.Copy(data, buffer, 0, size);
            Marshal.FreeHGlobal(data);

            return buffer;
        }

        private int ParseType(SimpleMessage t, byte[] buffer) {
            if (buffer.Length >= 1) {
                t.Type = buffer[0];

                this.ParseOffset += 1;
                this.parseState = ParseState.SIZE;

                return ParseStatus.OK;
            }

            return ParseStatus.PENDING;
        }

        private int ParseSize(SimpleMessage t, byte[] buffer) {
            if (buffer.Length >= (this.ParseOffset + 4))
            {
                BitConverter.ToInt32(buffer, this.ParseOffset);

                this.ParseOffset += 4;
                this.parseState = ParseState.ENTITY;

                return ParseStatus.OK;
            }

            return ParseStatus.PENDING;
        }

        private int ParseEntity(SimpleMessage t, byte[] buffer) {
            if (buffer.Length >= (this.ParseOffset + t.Size)) {
                t.Entity = System.Text.Encoding.UTF8.GetString(buffer, this.ParseOffset, t.Size);

                this.ParseOffset += t.Size;
                this.parseState = ParseState.TYPE;

                return ParseStatus.COMPLETE;
            }

            return ParseStatus.PENDING;
        }

        protected override int Parse(SimpleMessage t, byte[] buffer) {
            int result = ParseStatus.OK;

            switch (this.parseState) {
                case ParseState.TYPE:
                    result = this.ParseType(t, buffer);
                    break;

                case ParseState.SIZE:
                    result = this.ParseSize(t, buffer);
                    break;

                case ParseState.ENTITY:
                    result = this.ParseEntity(t, buffer);
                    break;
            }

            return result;
        }
    }
}
