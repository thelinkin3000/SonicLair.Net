using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicLairXbox.Types.SonicLair
{
    public class WebSocketMessage
    {
        public WebSocketMessage(string data, string type, string status)
        {
            Data = data;
            Type = type;
            Status = status;
        }
        public string Data { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }

    public class WebSocketCommand
    {
        public string Command { get; set; }
        public string Data { get; set; }
    }
    
    public class WebSocketNotification
    {
        public WebSocketNotification(string action, string value)
        {
            Action = action;
            Value = value;
        }

        public string Action { get; set; }
        public string Value { get; set; }
    }
}
