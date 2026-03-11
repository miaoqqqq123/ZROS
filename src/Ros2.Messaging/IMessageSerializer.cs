namespace Ros2.Messaging
{
    /// <summary>
    /// Interface for pluggable message serialization strategies.
    /// </summary>
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T message) where T : IMessage;
        T Deserialize<T>(byte[] data) where T : IMessage;
    }

    /// <summary>
    /// Base interface for all ROS 2 messages.
    /// </summary>
    public interface IMessage
    {
        string MessageType { get; }
    }
}
