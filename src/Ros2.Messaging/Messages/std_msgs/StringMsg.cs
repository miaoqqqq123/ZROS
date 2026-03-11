namespace Ros2.Messaging.Messages.std_msgs
{
    public class StringMsg : IMessage
    {
        public string MessageType => "std_msgs/String";
        public string Data { get; set; } = string.Empty;

        public StringMsg() { }
        public StringMsg(string data) { Data = data; }
    }
}
