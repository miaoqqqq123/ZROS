using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Zenoh.Native.Logging;

namespace Ros2.Core
{
    public class RosNode : IDisposable
    {
        private readonly List<string> _publishers = new List<string>();
        private readonly List<string> _subscribers = new List<string>();
        private readonly ILogger<RosNode> _logger;
        private bool _disposed;

        public string Name { get; }
        public string Namespace { get; }
        public RosContext Context { get; }

        internal RosNode(string name, RosContext context, RosNodeOptions? options = null, ILogger<RosNode>? logger = null)
        {
            Name = name;
            Context = context;
            Namespace = options?.Namespace ?? "/";
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<RosNode>();
            _logger.Info("Created node '{NodeName}' in namespace '{Namespace}'", Name, Namespace);
        }

        public void Spin()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosNode));
            _logger.Debug("Spinning node '{NodeName}' (simulation mode: {IsSimulated})", Name, Context.IsSimulated);
        }

        public void SpinOnce()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosNode));
            _logger.Debug("SpinOnce on node '{NodeName}' (simulation mode: {IsSimulated})", Name, Context.IsSimulated);
        }

        public void RegisterPublisher(string topic)
        {
            _publishers.Add(topic);
            _logger.Debug("Registered publisher on topic '{Topic}' for node '{NodeName}'", topic, Name);
        }

        public void RegisterSubscriber(string topic)
        {
            _subscribers.Add(topic);
            _logger.Debug("Registered subscriber on topic '{Topic}' for node '{NodeName}'", topic, Name);
        }

        public IReadOnlyList<string> Publishers => _publishers.AsReadOnly();
        public IReadOnlyList<string> Subscribers => _subscribers.AsReadOnly();

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _logger.Debug("Disposed node '{NodeName}'", Name);
            }
        }
    }
}
