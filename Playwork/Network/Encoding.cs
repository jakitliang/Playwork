using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Playwork.Network {
    interface IEncoding {
        int ParseOffset {
            get; set;
        }

        int ParseState {
            get; set;
        }

        int ParseStatus {
            get; set;
        }

        byte[] Generate();
        int Parse(byte[] buffer);
    }
    
    public class EncodingStatus {
        // Parse status
        public const int ERROR = -1;
        public const int CONTINUE = 0;
        public const int PENDING = 1;
        public const int COMPLETE = 2;
    }

    public class EncodingState {
        // Parse state
        public const int BEGIN = 0;
    }

    class Codec<T> where T : IEncoding {
        public static byte[] Encode(T t) {
            return t.Generate();
        }

        public static int Decode(T t, byte[] buffer) {
            int status = EncodingStatus.CONTINUE;

            while (status == EncodingStatus.CONTINUE) {
                Console.WriteLine("Parsing continue ...");
                status = t.Parse(buffer);
            }

            if (status == EncodingStatus.PENDING) {
                status = EncodingStatus.CONTINUE;
            }

            int offset = t.ParseOffset;
            t.ParseStatus = status;
            t.ParseOffset = 0;

            return offset;
        }

        public static bool IsComplete(T t) {
            return t.ParseStatus == EncodingStatus.COMPLETE;
        }
    }
}
