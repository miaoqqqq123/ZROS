using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZROS.Core.Logging;

namespace ZROS.Core
{
    public class RosActionServer<TGoal, TFeedback, TResult> : IDisposable
        where TGoal : IMessage
        where TFeedback : IMessage
        where TResult : IMessage
    {
        private readonly RosNode _node;
        private readonly Func<TGoal, bool> _goalCallback;
        private readonly Func<TGoal, CancellationToken, Task<TResult>> _executeCallback;
        private readonly Action<TFeedback>? _feedbackPublisher;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<RosActionServer<TGoal, TFeedback, TResult>> _logger;
        private bool _disposed;

        public string ActionName { get; }
        public bool IsRunning { get; private set; }

        public RosActionServer(
            RosNode node,
            string actionName,
            Func<TGoal, bool> goalCallback,
            Func<TGoal, CancellationToken, Task<TResult>> executeCallback,
            Action<TFeedback>? feedbackPublisher = null,
            IMessageSerializer? serializer = null,
            ILogger<RosActionServer<TGoal, TFeedback, TResult>>? logger = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
            _goalCallback = goalCallback ?? throw new ArgumentNullException(nameof(goalCallback));
            _executeCallback = executeCallback ?? throw new ArgumentNullException(nameof(executeCallback));
            _feedbackPublisher = feedbackPublisher;
            _serializer = serializer ?? new JsonMessageSerializer();
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<RosActionServer<TGoal, TFeedback, TResult>>();

            IsRunning = true;

            if (_node.Context.IsSimulated)
            {
                _logger.Info("[SIM] Action server '{ActionName}' is running (simulation mode)", ActionName);
            }
            else
            {
                _logger.Info("Action server '{ActionName}' started on node '{NodeName}'", ActionName, _node.Name);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                IsRunning = false;
                _logger.Debug("Disposed action server for '{ActionName}'", ActionName);
            }
        }
    }
}
