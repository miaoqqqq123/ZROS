using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ros2.Core;
using Ros2.Messaging;
using Zenoh.Native.Logging;

namespace Ros2.Actions
{
    public class ActionGoalHandle<TFeedback, TResult>
    {
        public ActionGoalStatus Status { get; internal set; }
        public Guid GoalId { get; } = Guid.NewGuid();

        private readonly bool _isSimulated;
        private readonly ILogger<ActionGoalHandle<TFeedback, TResult>> _logger;

        internal ActionGoalHandle(bool isSimulated, ILogger<ActionGoalHandle<TFeedback, TResult>> logger)
        {
            _isSimulated = isSimulated;
            _logger = logger;
            Status = ActionGoalStatus.Accepted;
        }

        public async Task<TResult?> WaitForResultAsync(CancellationToken cancellationToken = default)
        {
            if (_isSimulated)
            {
                _logger.Debug("[SIM] Waiting for result of goal {GoalId}", GoalId);
                await Task.Yield();
                Status = ActionGoalStatus.Succeeded;
                return default;
            }
            throw new NotImplementedException("Real zenoh action result waiting not yet implemented.");
        }

        public async Task<bool> CancelAsync()
        {
            if (_isSimulated)
            {
                _logger.Debug("[SIM] Canceling goal {GoalId}", GoalId);
                await Task.Yield();
                Status = ActionGoalStatus.Canceled;
                return true;
            }
            throw new NotImplementedException("Real zenoh action cancellation not yet implemented.");
        }
    }

    public class RosActionClient<TGoal, TFeedback, TResult> : IDisposable
        where TGoal : IMessage
        where TFeedback : IMessage
        where TResult : IMessage
    {
        private readonly RosNode _node;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<RosActionClient<TGoal, TFeedback, TResult>> _logger;
        private bool _disposed;

        public string ActionName { get; }

        public RosActionClient(RosNode node, string actionName, IMessageSerializer? serializer = null, ILogger<RosActionClient<TGoal, TFeedback, TResult>>? logger = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
            _serializer = serializer ?? new JsonMessageSerializer();
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<RosActionClient<TGoal, TFeedback, TResult>>();
            _logger.Info("Created action client for '{ActionName}' on node '{NodeName}'", ActionName, _node.Name);
        }

        public async Task<ActionGoalHandle<TFeedback, TResult>> SendGoalAsync(TGoal goal)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosActionClient<TGoal, TFeedback, TResult>));
            if (goal == null) throw new ArgumentNullException(nameof(goal));

            if (_node.Context.IsSimulated)
            {
                _logger.Debug("[SIM] Sending goal to action '{ActionName}'", ActionName);
                await Task.Yield();
                var handleLogger = ZrosLoggerFactory.CreateLogger<ActionGoalHandle<TFeedback, TResult>>();
                return new ActionGoalHandle<TFeedback, TResult>(isSimulated: true, handleLogger)
                {
                    Status = ActionGoalStatus.Accepted
                };
            }

            throw new NotImplementedException("Real zenoh action goal sending not yet implemented.");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _logger.Debug("Disposed action client for '{ActionName}'", ActionName);
            }
        }
    }
}
