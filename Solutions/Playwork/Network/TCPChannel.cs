using System;
using System.Net;
using System.Net.Sockets;

namespace Playwork.Network {
    public class TCPSocket : IO {
        private IPEndPoint endPoint;

        public bool Blocking { get { return Socket.Blocking; } set { Socket.Blocking = value; } }

        public Socket Socket { get; }

        public TCPSocket(string serverIP, int port) {
            endPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //socket.Blocking = false;

            try {
                Socket.Connect(this.endPoint);
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        ~TCPSocket() {
            Close();
        }

        public void Close() {
            if (!Socket.Connected) {
                return;
            }

            try {
                Socket.Close();
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        public int Read(byte[] buffer, int size) {
            if (!Socket.Blocking) {
                if (size > Socket.Available) {
                    size = Socket.Available;
                }

                return Socket.Receive(buffer, size, SocketFlags.None);
            }

            return Socket.Receive(buffer, (int)size, SocketFlags.None);
        }

        public int Write(byte[] buffer, int size) {
            return Socket.Send(buffer);
        }
    }

    public class TCPChannel : Channel {
        public TCPChannel(string serverIP, int port) : base(new TCPSocket(serverIP, port), null) {

        }

        public TCPChannel(string serverIP, int port, IChannelHandler handler) : base(new TCPSocket(serverIP, port), handler) {

        }
    }
}