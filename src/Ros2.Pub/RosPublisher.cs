using System;
using Ros2.Core;
using Ros2.Messaging;

namespace Ros2.Pub
{
    public class RosPublisher<T> : IDisposable where T : IMessage
    {
        private readonly RosNode _node;
        private readonly IMessageSerializer _serializer;
        private bool _disposed;

        public string Topic { get; }
        public string NodeName => _node.Name;
        public int PublishedCount { get; private set; }

        public RosPublisher(RosNode node, string topic, IMessageSerializer? serializer = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
            _serializer = serializer ?? new JsonMessageSerializer();
            _node.RegisterPublisher(topic);
            Console.WriteLine($"[RosPublisher] Created publisher on topic '{Topic}' for node '{NodeName}'");
        }

        public void Publish(T message)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosPublisher<T>));
            if (message == null) throw new ArgumentNullException(nameof(message));

            byte[] payload = _serializer.Serialize(message);
            PublishedCount++;

            if (_node.Context.IsSimulated)
            {
                Console.WriteLine($"[RosPublisher] [SIM] Published message #{PublishedCount} on topic '{Topic}': {System.Text.Encoding.UTF8.GetString(payload)}");
            }
            else
            {
                Console.WriteLine($"[RosPublisher] Published message #{PublishedCount} on topic '{Topic}' ({payload.Length} bytes)");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Console.WriteLine($"[RosPublisher] Disposed publisher on topic '{Topic}'");
            }
        }
    }
}
