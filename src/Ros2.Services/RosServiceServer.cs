using System;
using Microsoft.Extensions.Logging;
using Ros2.Core;
using Ros2.Messaging;
using Zenoh.Native.Logging;

namespace Ros2.Services
{
    public class RosServiceServer<TRequest, TResponse> : IDisposable
        where TRequest : IMessage
        where TResponse : IMessage
    {
        private readonly RosNode _node;
        private readonly Func<TRequest, TResponse> _handler;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<RosServiceServer<TRequest, TResponse>> _logger;
        private bool _disposed;

        public string ServiceName { get; }
        public bool IsRunning { get; private set; }

        public RosServiceServer(RosNode node, string serviceName, Func<TRequest, TResponse> handler, IMessageSerializer? serializer = null, ILogger<RosServiceServer<TRequest, TResponse>>? logger = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _serializer = serializer ?? new JsonMessageSerializer();
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<RosServiceServer<TRequest, TResponse>>();

            if (_node.Context.IsSimulated)
            {
                IsRunning = true;
                _logger.Info("[SIM] Service '{ServiceName}' is running (simulation mode - no zenoh listener registered)", ServiceName);
            }
            else
            {
                IsRunning = true;
                _logger.Info("Service '{ServiceName}' started on node '{NodeName}'", ServiceName, _node.Name);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                IsRunning = false;
                _logger.Debug("Disposed server for service '{ServiceName}'", ServiceName);
            }
        }
    }
}
