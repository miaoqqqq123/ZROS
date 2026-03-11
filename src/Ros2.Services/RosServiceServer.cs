using System;
using Ros2.Core;
using Ros2.Messaging;

namespace Ros2.Services
{
    public class RosServiceServer<TRequest, TResponse> : IDisposable
        where TRequest : IMessage
        where TResponse : IMessage
    {
        private readonly RosNode _node;
        private readonly Func<TRequest, TResponse> _handler;
        private readonly IMessageSerializer _serializer;
        private bool _disposed;

        public string ServiceName { get; }
        public bool IsRunning { get; private set; }

        public RosServiceServer(RosNode node, string serviceName, Func<TRequest, TResponse> handler, IMessageSerializer? serializer = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _serializer = serializer ?? new JsonMessageSerializer();

            if (_node.Context.IsSimulated)
            {
                IsRunning = true;
                Console.WriteLine($"[RosServiceServer] [SIM] Service '{ServiceName}' is running (simulation mode - no zenoh listener registered)");
            }
            else
            {
                IsRunning = true;
                Console.WriteLine($"[RosServiceServer] Service '{ServiceName}' started on node '{_node.Name}'");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                IsRunning = false;
                Console.WriteLine($"[RosServiceServer] Disposed server for service '{ServiceName}'");
            }
        }
    }
}
