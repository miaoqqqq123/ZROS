using System;
using System.Collections.Generic;

namespace Ros2.Core
{
    public class RosNode : IDisposable
    {
        private readonly List<string> _publishers = new List<string>();
        private readonly List<string> _subscribers = new List<string>();
        private bool _disposed;

        public string Name { get; }
        public string Namespace { get; }
        public RosContext Context { get; }

        internal RosNode(string name, RosContext context, RosNodeOptions? options = null)
        {
            Name = name;
            Context = context;
            Namespace = options?.Namespace ?? "/";
            Console.WriteLine($"[RosNode] Created node '{Name}' in namespace '{Namespace}'");
        }

        public void Spin()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosNode));
            Console.WriteLine($"[RosNode] Spinning node '{Name}' (simulation mode: {Context.IsSimulated})");
        }

        public void SpinOnce()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosNode));
            Console.WriteLine($"[RosNode] SpinOnce on node '{Name}' (simulation mode: {Context.IsSimulated})");
        }

        public void RegisterPublisher(string topic)
        {
            _publishers.Add(topic);
            Console.WriteLine($"[RosNode] Registered publisher on topic '{topic}' for node '{Name}'");
        }

        public void RegisterSubscriber(string topic)
        {
            _subscribers.Add(topic);
            Console.WriteLine($"[RosNode] Registered subscriber on topic '{topic}' for node '{Name}'");
        }

        public IReadOnlyList<string> Publishers => _publishers.AsReadOnly();
        public IReadOnlyList<string> Subscribers => _subscribers.AsReadOnly();

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Console.WriteLine($"[RosNode] Disposed node '{Name}'");
            }
        }
    }
}
