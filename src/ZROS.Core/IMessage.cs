namespace ZROS.Core
{
    /// <summary>
    /// Base interface for all ZROS messages.
    /// </summary>
    public interface IMessage
    {
        string MessageType { get; }
    }

    /// <summary>
    /// Interface for pluggable message serialization strategies.
    /// </summary>
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T message) where T : IMessage;
        T Deserialize<T>(byte[] data) where T : IMessage;
    }
}
