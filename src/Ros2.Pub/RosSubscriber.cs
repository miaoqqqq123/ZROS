using System;
using Microsoft.Extensions.Logging;
using Ros2.Core;
using Ros2.Messaging;
using Zenoh.Native.Logging;

namespace Ros2.Pub
{
    public class RosSubscriber<T> : IDisposable where T : IMessage
    {
        private readonly RosNode _node;
        private readonly Action<T> _callback;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<RosSubscriber<T>> _logger;
        private bool _disposed;

        public string Topic { get; }
        public string NodeName => _node.Name;
        public int ReceivedCount { get; private set; }

        public RosSubscriber(RosNode node, string topic, Action<T> callback, IMessageSerializer? serializer = null, ILogger<RosSubscriber<T>>? logger = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _serializer = serializer ?? new JsonMessageSerializer();
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<RosSubscriber<T>>();
            _node.RegisterSubscriber(topic);
            _logger.Info("Created subscriber on topic '{Topic}' for node '{NodeName}'", Topic, NodeName);
        }

        /// <summary>
        /// Simulates receiving a message by deserializing the given bytes and invoking the callback.
        /// Used for testing without a live zenoh session.
        /// </summary>
        public void SimulateReceive(byte[] data)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosSubscriber<T>));
            if (data == null) throw new ArgumentNullException(nameof(data));

            T message = _serializer.Deserialize<T>(data);
            ReceivedCount++;
            _logger.Debug("[SIM] Received message #{Count} on topic '{Topic}'", ReceivedCount, Topic);
            _callback(message);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _logger.Debug("Disposed subscriber on topic '{Topic}'", Topic);
            }
        }
    }
}
