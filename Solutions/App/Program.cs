using Newtonsoft.Json;
using Playwork.Network;
using Playwork.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace App {
    class TestClient : IChannelHandler {
        public TCPChannel Channel { get; }
        private int count;

        public TestClient() {
            count = 0;
            Channel = new TCPChannel("127.0.0.1", 10086, this);
        }

        public void OnReady(Channel channel) {
            Console.WriteLine("Channel {0} is OnReady", channel);
        }

        public int OnReceive(Channel channel, byte[] buffer) {
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer));

            channel.Write(buffer, buffer.Length);

            return buffer.Length;
        }

        public void OnSend(Channel channel) {
            count += 1;
            Console.WriteLine("Message sent ok {0}", count);
            return;
        }

        public void Tell() {
            byte[] msg = System.Text.Encoding.UTF8.GetBytes("Test");
            Channel.Write(msg, msg.Length);
        }
    }

    class TestWSClient : IWebsocketClientHandler {
        private WebsocketClient cli;

        public TestWSClient() {
            cli = new WebsocketClient("127.0.0.1", 3001, this);
        }

        public void OnConnect(WebsocketClient client) {
            Console.Write("OnConnect: ");
            Console.WriteLine(client);
        }

        public void OnDisconnect(WebsocketClient client) {
            Console.Write("OnDisconnect: ");
            Console.WriteLine(client);
        }

        public void OnMessage(WebsocketClient client, string message) {
            Console.Write("OnMessage: ");
            Console.WriteLine(client);
        }

        public void Tell() {
            cli.Send("Test");
        }
    }

    class UDPTest : IChannelHandler {
        public UDPServerChannel Server { get; set; }
        public UDPChannel Client { get; set; }

        public UDPTest() {
            Server = new UDPServerChannel("127.0.0.1", 0, this);
            Client = new UDPChannel("127.0.0.1", ((IPEndPoint) Server.IO.Socket.LocalEndPoint).Port, this);
        }

        public void OnReady(Channel channel) {
            Console.WriteLine("Channel ready");
        }

        public int OnReceive(Channel channel, byte[] buffer) {
            Console.WriteLine("Channel receive: {0}", System.Text.Encoding.UTF8.GetString(buffer));

            return buffer.Length;
        }

        public void OnSend(Channel channel) {
            Console.WriteLine("Channel ready");
        }

        public void Tell() {
            byte[] b = System.Text.Encoding.UTF8.GetBytes("nihao");
            Client.Write(b, b.Length);
        }
    }

    class SampleProgram : IAgentHandler {
        private Agent agent;
        private int count;

        public SampleProgram() {
            agent = new Agent(this);
        }

        public int ProcessApi(Message<APITaskContent> apiMessage) {
            Console.WriteLine("SampleProgram process");
            count += 1;
            return count;
        }
    }

    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Begin: ");
            Console.ReadKey();

            Console.WriteLine("Start: ");

            //TestClient client = new TestClient();
            //while (true) {
            //    Thread.Sleep(2000);
            //    client.Tell();
            //}

            //TCPChannel channel = new TCPChannel("127.0.0.1", 10086, client);

            //while (true)
            //{
            //    string buffer = Console.ReadLine();

            //    if (buffer.Length > 5)
            //    {
            //        break;
            //    }
            //}

            //WebsocketProto proto = new WebsocketProto();
            //byte[] wsData = new byte[] { 129, 134, 167, 225, 225, 210, 198, 131, 130, 182, 194, 135 };

            //int result = Codec<WebsocketProto>.Decode(proto, wsData);

            //WebsocketProto msg = new WebsocketProto();
            //msg.Payload = "abcdef";
            //msg.Masking = proto.Masking;
            //msg.IsMask = true;
            //msg.IsFin = true;
            //msg.Operation = proto.Operation;

            //byte[] encoded = Codec<WebsocketProto>.Encode(msg);

            //WebsocketClient client = new WebsocketClient("127.0.0.1", 3001);

            //Message<APITaskContent> msg = JsonConvert.DeserializeObject<Message<APITaskContent>>("{\"target\":\"jNntZU\",\"from\":\"0vk7s1\",\"content_type\":\"api\",\"content\":{\"id\":1,\"type\":1,\"call\":\"create_engine\",\"params\":{}}}");

            //TestWSClient cli = new TestWSClient();

            //while (true) {
            //    Thread.Sleep(2000);
            //    cli.Tell();
            //    Console.WriteLine("......");
            //}

            //UDPTest t = new UDPTest();

            //while (true) {
            //    Thread.Sleep(2000);
            //    t.Tell();
            //}

            SampleProgram program = new SampleProgram();

            Console.WriteLine("Done: ");
            Console.ReadKey();
            Console.WriteLine("Exit");
        }
    }
}
