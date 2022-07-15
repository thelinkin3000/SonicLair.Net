namespace SonicLair.Lib.Types.SonicLair
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
