using System;
using Microsoft.Extensions.Logging;
using ZROS.Core.Logging;

namespace ZROS.Core
{
    public class RosPublisher<T> : IDisposable where T : IMessage
    {
        private readonly RosNode _node;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<RosPublisher<T>> _logger;
        private bool _disposed;

        public string Topic { get; }
        public string NodeName => _node.Name;
        public int PublishedCount { get; private set; }

        public RosPublisher(RosNode node, string topic, IMessageSerializer? serializer = null, ILogger<RosPublisher<T>>? logger = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
            _serializer = serializer ?? new JsonMessageSerializer();
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<RosPublisher<T>>();
            _node.RegisterPublisher(topic);
            _logger.Info("Created publisher on topic '{Topic}' for node '{NodeName}'", Topic, NodeName);
        }

        public void Publish(T message)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosPublisher<T>));
            if (message == null) throw new ArgumentNullException(nameof(message));

            byte[] payload = _serializer.Serialize(message);
            PublishedCount++;

            if (_node.Context.IsSimulated)
            {
                _logger.Debug("[SIM] Published message #{Count} on topic '{Topic}': {Payload}",
                    PublishedCount, Topic, System.Text.Encoding.UTF8.GetString(payload));
            }
            else
            {
                _logger.Debug("Published message #{Count} on topic '{Topic}' ({Bytes} bytes)",
                    PublishedCount, Topic, payload.Length);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _logger.Debug("Disposed publisher on topic '{Topic}'", Topic);
            }
        }
    }
}
