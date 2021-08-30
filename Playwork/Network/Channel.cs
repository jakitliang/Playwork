using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Playwork.Network {
    public interface IO
    {
        Socket Socket { get; }
        bool Blocking { get; set; }
        int Read(byte[] buffer, int size);
        int Write(byte[] buffer, int size);
        void Close();
    }

    public interface IChannelHandler
    {
        void OnReady(Channel channel);
        int OnReceive(Channel channel, byte[] buffer);
        void OnSend(Channel channel);
    }

    public class Channel : IEventHandler {
        public IO IO { get; }
        public IChannelHandler Handler { get; set; }

        private byte[] readBuffer;
        private byte[] writeBuffer;
        private List<int> writeCount;

        private Mutex writeLock;

        public Channel(IO io, IChannelHandler handler)
        {
            this.IO = io;
            this.Handler = handler;
            readBuffer = new byte[0];
            writeBuffer = new byte[0];
            writeLock = new Mutex();
            writeCount = new List<int>();

            //this.io.Blocking = false;

            Reactor.Attach(io, this, Reactor.FLAG_READ);
        }

        public int OnRead(IO io)
        {
            const int readSize = 2 * 1024 * 1024;
            byte[] buffer = new byte[readSize];
            int len;

            try
            {
                len = io.Read(buffer, readSize);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return Reactor.HANDLE_FAILED;
            }

            if (len <= 0)
            {
                Console.WriteLine("Channel on read close");
                return Reactor.HANDLE_FAILED;
            }

            byte[] newReadBuffer = new byte[readBuffer.Length + len];

            Buffer.BlockCopy(readBuffer, 0, newReadBuffer, 0, readBuffer.Length);
            Buffer.BlockCopy(buffer, 0, newReadBuffer, readBuffer.Length, len);

            readBuffer = newReadBuffer;

            if (Handler != null)
            {
                len = Handler.OnReceive(this, readBuffer);

                if (len > 0) {
                    if (len > readBuffer.Length) {
                        len = readBuffer.Length;
                    }

                    newReadBuffer = new byte[readBuffer.Length - len];
                    Buffer.BlockCopy(readBuffer, len, newReadBuffer, 0, readBuffer.Length - len);

                    readBuffer = newReadBuffer;

                } else if (len < 0) {
                    return Reactor.HANDLE_FAILED;
                }
            }

            return Reactor.HANDLE_CONTINUED;
        }

        public int OnWrite(IO io)
        {
            int len;

            writeLock.WaitOne(); // begin

            try
            {
                len = io.Write(writeBuffer, writeBuffer.Length);
            }
            catch (Exception e)
            {
                writeLock.ReleaseMutex(); // end
                Console.WriteLine(e.ToString());
                return Reactor.HANDLE_FAILED;
            }

            byte[] newWriteBuffer = new byte[writeBuffer.Length - len];

            Buffer.BlockCopy(writeBuffer, len, newWriteBuffer, 0, writeBuffer.Length - len);
            writeBuffer = newWriteBuffer;

            UpdateWriteCount(len);

            if (writeBuffer.Length == 0)
            {
                Console.WriteLine("Channel on write finished");
                writeLock.ReleaseMutex(); // end
                return Reactor.HANDLE_FINISHED;
            }

            writeLock.ReleaseMutex(); // end
            return Reactor.HANDLE_CONTINUED;
        }

        public void OnDetach(IO io)
        {
            try
            {
                io.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void UpdateWriteCount(int size) {
            while (size > 0) {
                if (writeCount.Count() < 1) {
                    return;
                }

                int first = writeCount[0];

                if (size >= first) {
                    size -= first;
                    writeCount.RemoveAt(0);

                    if (Handler != null) {
                        Console.WriteLine("Channel Sent finished");
                        Handler.OnSend(this);
                    }

                    continue;
                }

                first -= size;
                writeCount[0] = first;
            }
        }

        public void Write(byte[] buffer, int size)
        {
            writeLock.WaitOne(); // begin

            byte[] newWriteBuffer = new byte[writeBuffer.Length + size];

            Buffer.BlockCopy(writeBuffer, 0, newWriteBuffer, 0, writeBuffer.Length);
            Buffer.BlockCopy(buffer, 0, newWriteBuffer, writeBuffer.Length, size);

            writeBuffer = newWriteBuffer;
            writeCount.Add(buffer.Count());

            writeLock.ReleaseMutex(); // end

            Reactor.Modify(IO, this, Reactor.FLAG_READ | Reactor.FLAG_WRITE);
        }

        public void OnAttach(IO io) {
            if (Handler != null) {
                Handler.OnReady(this);
            }
        }
    }
}
