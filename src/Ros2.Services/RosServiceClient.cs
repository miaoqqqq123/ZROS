using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ros2.Core;
using Ros2.Messaging;
using Zenoh.Native.Logging;

namespace Ros2.Services
{
    public class RosServiceClient<TRequest, TResponse> : IDisposable
        where TRequest : IMessage
        where TResponse : IMessage
    {
        private readonly RosNode _node;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<RosServiceClient<TRequest, TResponse>> _logger;
        private bool _disposed;

        public string ServiceName { get; }

        public RosServiceClient(RosNode node, string serviceName, IMessageSerializer? serializer = null, ILogger<RosServiceClient<TRequest, TResponse>>? logger = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _serializer = serializer ?? new JsonMessageSerializer();
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<RosServiceClient<TRequest, TResponse>>();
            _logger.Info("Created client for service '{ServiceName}' on node '{NodeName}'", ServiceName, _node.Name);
        }

        public async Task<TResponse> CallAsync(TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosServiceClient<TRequest, TResponse>));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(1);

            if (_node.Context.IsSimulated)
            {
                _logger.Warn("[SIM] Calling service '{ServiceName}' (will timeout after {Timeout}s)", ServiceName, effectiveTimeout.TotalSeconds);
                await Task.Delay(effectiveTimeout, cancellationToken);
                throw new NotSupportedException("Service call not supported in simulation mode");
            }

            // Real implementation would use zenoh z_get here
            throw new NotImplementedException("Real zenoh service calls not yet implemented.");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _logger.Debug("Disposed client for service '{ServiceName}'", ServiceName);
            }
        }
    }
}
