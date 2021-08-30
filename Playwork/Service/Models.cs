
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Playwork.Service {
    public class BasicMessage {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; } // Symbol for machine

        [JsonProperty("content_type")]
        public string ContentType { get; set; } // Message content type

        [JsonIgnore]
        public string Origin { get; set; }
    }

    public class Message : BasicMessage {
        public Message() {
            From = "";
            Target = "";
            ContentType = "";
            Origin = "";
        }

        public static Message Parse(string buffer) {
            Message message = null;

            try {
                message = JsonConvert.DeserializeObject<Message>(buffer);
                message.Origin = buffer;

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

            return message;
        }
    }

    public class Message<T> :
        BasicMessage
        where T : new() {
        [JsonProperty("content")]
        public T Content { get; set; } // Message content

        public Message() {
            Content = new T();
        }

        public static Message<T> Parse(string buffer) {
            Message<T> message = null;

            try {
                message = JsonConvert.DeserializeObject<Message<T>>(buffer);
                message.Origin = buffer;

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

            return message;
        }
    }

    public class LoginContent {
        [JsonProperty("members")]
        public List<string> Members { get; set; }
    }

    public class TaskContent {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }
    }

    public class APITaskContent : TaskContent {
        [JsonProperty("call")]
        public string Call { get; set; }

        [JsonProperty("params")]
        public string[] Parameters { get; set; }
    }

    public class APITaskContent<T> : TaskContent {
        [JsonProperty("call")]
        public string Call {
            get; set;
        }

        [JsonProperty("params")]
        public T Parameters { get; set; }
    }

    public class LogContent {
        [JsonProperty("log_list")]
        public List<LogItem> LogList { get; set; }
    }

    public class LogItem {
        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class ResultContent {
        [JsonProperty("id")]
        public int ID { get; set; }
    }

    public class APIResultContent : ResultContent {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}