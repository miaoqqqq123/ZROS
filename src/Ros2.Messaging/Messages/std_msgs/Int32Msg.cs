namespace Ros2.Messaging.Messages.std_msgs
{
    public class Int32Msg : IMessage
    {
        public string MessageType => "std_msgs/Int32";
        public int Data { get; set; }

        public Int32Msg() { }
        public Int32Msg(int data) { Data = data; }
    }
}
