using System;
using System.Threading;
using System.Threading.Tasks;
using Ros2.Core;
using Ros2.Messaging;

namespace Ros2.Actions
{
    public class ActionGoalHandle<TFeedback, TResult>
    {
        public ActionGoalStatus Status { get; internal set; }
        public Guid GoalId { get; } = Guid.NewGuid();

        private readonly bool _isSimulated;

        internal ActionGoalHandle(bool isSimulated)
        {
            _isSimulated = isSimulated;
            Status = ActionGoalStatus.Accepted;
        }

        public async Task<TResult?> WaitForResultAsync(CancellationToken cancellationToken = default)
        {
            if (_isSimulated)
            {
                Console.WriteLine($"[ActionGoalHandle] [SIM] Waiting for result of goal {GoalId}");
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
                Console.WriteLine($"[ActionGoalHandle] [SIM] Canceling goal {GoalId}");
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
        private bool _disposed;

        public string ActionName { get; }

        public RosActionClient(RosNode node, string actionName, IMessageSerializer? serializer = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
            _serializer = serializer ?? new JsonMessageSerializer();
            Console.WriteLine($"[RosActionClient] Created action client for '{ActionName}' on node '{_node.Name}'");
        }

        public async Task<ActionGoalHandle<TFeedback, TResult>> SendGoalAsync(TGoal goal)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosActionClient<TGoal, TFeedback, TResult>));
            if (goal == null) throw new ArgumentNullException(nameof(goal));

            if (_node.Context.IsSimulated)
            {
                Console.WriteLine($"[RosActionClient] [SIM] Sending goal to action '{ActionName}'");
                await Task.Yield();
                return new ActionGoalHandle<TFeedback, TResult>(isSimulated: true)
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
                Console.WriteLine($"[RosActionClient] Disposed action client for '{ActionName}'");
            }
        }
    }
}
