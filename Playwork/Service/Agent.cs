
using Playwork.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Playwork.Service {
    public interface IAgentHandler {
        int ProcessApi(Message<APITaskContent> apiMessage);
    }

    public class Agent : IWebsocketClientHandler {
        private WebsocketClient client;
        public string ID { get; set; }
        public IAgentHandler Handler { get; set; }
        public string BossID { get; set; }

        public Agent(IAgentHandler handler) {
            client = new WebsocketClient("127.0.0.1", 3001, this);
            Handler = handler;
        }

        public Agent() : this(null) { }

        public void OnConnect(WebsocketClient client) {
            //throw new System.NotImplementedException();
        }

        public void OnDisconnect(WebsocketClient client) {
            //throw new System.NotImplementedException();
        }

        public void OnMessage(WebsocketClient client, string message) {
            try {
                Message msg = Message.Parse(message);

                if (msg == null) {
                    return;
                }

                if (msg.ContentType == "login") {
                    ID = msg.Target;

                    Console.Write("Login to garnet with ID: ");
                    Console.WriteLine(ID);

                } else if (msg.ContentType == "api") {
                    Console.Write("API Task is coming, from ID: ");
                    Console.WriteLine(msg.Target);

                    Message<APITaskContent> apiMessage = Message<APITaskContent>.Parse(message);

                    OnApiCall(apiMessage);

                    Console.Write("API name: ");
                    Console.WriteLine(apiMessage.Content.Call);
                }

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public void OnApiCall(Message<APITaskContent> apiMessage) {
            if (Handler != null) {
                int result = Handler.ProcessApi(apiMessage);

                Message<APIResultContent> apiResultMessage = new Message<APIResultContent>();
                apiResultMessage.Target = apiMessage.From;
                apiResultMessage.From = ID;
                apiResultMessage.ContentType = "api_result";
                apiResultMessage.Content.ID = apiMessage.Content.ID;
                apiResultMessage.Content.Status = result;
                apiResultMessage.Content.Message = "OK";

                string msg = JsonConvert.SerializeObject(apiResultMessage);
                client.Send(msg);
            }
        }

        public void StatusReport() {

        }

        public void LogReport(List<LogItem> logList) {
            Message<LogContent> logMessages = new Message<LogContent>();
            logMessages.From = ID;
            logMessages.Target = BossID;
            logMessages.ContentType = "log";
            logMessages.Content.LogList = logList;

            string msg = JsonConvert.SerializeObject(logMessages);
            client.Send(msg);
        }
    }
}