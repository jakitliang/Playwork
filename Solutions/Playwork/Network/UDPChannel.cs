using System;
using System.Net;
using System.Net.Sockets;

namespace Playwork.Network {
    public class UDPSocket : IO {
        public IPEndPoint EndPoint { get; }

        public Socket Socket { get; }

        public bool Blocking { get { return Socket.Blocking; } set { Socket.Blocking = value; } }

        public UDPSocket(string ip, int port) {
            EndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try {
                Socket.Connect(EndPoint);

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public UDPSocket(IPEndPoint endPoint) {
            EndPoint = endPoint;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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

    public class UDPServer : UDPSocket {
        public UDPServer(string serverIP, int port) : base(new IPEndPoint(IPAddress.Parse(serverIP), port)) {
            Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true); ;
            Socket.Bind(EndPoint);
        }
    }

    public class UDPChannel : Channel {
        public UDPChannel(string ip, int port, IChannelHandler handler) : base(new UDPSocket(ip, port), handler) {
            
        }

        public UDPChannel(string ip, int port) : this(ip, port, null) {

        }
    }

    public class UDPServerChannel : Channel {
        public UDPServerChannel(string serverIP, int port, IChannelHandler handler) : base(new UDPServer(serverIP, port), handler) {

        }
    }
}