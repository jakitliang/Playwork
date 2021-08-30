using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Playwork.Network {
    public interface IWebsocketClientHandler {
        void OnConnect(WebsocketClient client);
        void OnMessage(WebsocketClient client, string message);
        void OnDisconnect(WebsocketClient client);
    }

    public class WebsocketClient : IChannelHandler {
        private TCPChannel channel;
        private bool upgraded;
        private IWebsocketClientHandler handler;
        private const byte BYTE_CR = 13;
        private const byte BYTE_LF = 10;

        public WebsocketClient(string ip, int port) {
            channel = new TCPChannel(ip, port, this);
            upgraded = false;
            handler = null;
        }

        public WebsocketClient(string ip, int port, IWebsocketClientHandler handler) : this(ip, port) {
            this.handler = handler;
        }

        private int ParseHttpResponse(byte[] httpResponse) {
            for (int i = 0; i < httpResponse.Length; ++i) {
                if (httpResponse[i] == BYTE_CR) {
                    bool result = httpResponse[i + 1] == BYTE_LF
                        && httpResponse[i + 2] == BYTE_CR
                        && httpResponse[i + 3] == BYTE_LF;

                    if (result) {
                        Console.WriteLine("Connect ok");
                        upgraded = true;

                        if (handler != null) {
                            handler.OnConnect(this);
                        }

                        return i + 4;
                    }
                }
            }

            return 0;
        }

        public int OnReceive(Channel channel, byte[] buffer) {
            int available = buffer.Length;
            int offset = 0;

            if (!upgraded) {
                offset = ParseHttpResponse(buffer);
                available = buffer.Length - offset;

                if (offset == 0) {
                    return 0;
                }
            }
            
            while (available > 0) {
                WebsocketProto message = new WebsocketProto();
                message.ParseOffset = offset;
                offset = Codec<WebsocketProto>.Decode(message, buffer);
                available = buffer.Length - offset;

                if (message.ParseStatus == EncodingStatus.COMPLETE) {
                    Console.Write("Websocket receive: ");
                    Console.WriteLine(message.Payload);

                    if (handler != null) {
                        handler.OnMessage(this, message.Payload);
                    }

                } else if (message.ParseStatus == EncodingStatus.ERROR) {
                    return -1;

                } else if (message.ParseStatus == EncodingStatus.PENDING) {
                    break;
                }
            }

            return offset;
        }

        public void OnSend(Channel channel) {
            return;
        }

        private void Handshake() {
            string msg = "GET / HTTP/1.1\r\n" +
                "Upgrade: websocket\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                "Sec-WebSocket-Version: 13\r\n\r\n";

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
            channel.Write(buffer, buffer.Length);
        }

        public void Send(string message) {
            WebsocketProto msg = new WebsocketProto();
            msg.Payload = message;
            msg.IsMask = true;
            msg.IsFin = true;
            msg.RefreshMasking();
            msg.Operation = WebsocketMessage.OP_TEXT;

            byte[] buffer = msg.Generate();
            channel.Write(buffer, buffer.Length);
        }

        public void OnReady(Channel channel) {
            Handshake();
        }
    }
}
