using System;
using System.Threading;
using System.Threading.Tasks;
using Ros2.Core;
using Ros2.Messaging;

namespace Ros2.Actions
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
        private bool _disposed;

        public string ActionName { get; }
        public bool IsRunning { get; private set; }

        public RosActionServer(
            RosNode node,
            string actionName,
            Func<TGoal, bool> goalCallback,
            Func<TGoal, CancellationToken, Task<TResult>> executeCallback,
            Action<TFeedback>? feedbackPublisher = null,
            IMessageSerializer? serializer = null)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
            _goalCallback = goalCallback ?? throw new ArgumentNullException(nameof(goalCallback));
            _executeCallback = executeCallback ?? throw new ArgumentNullException(nameof(executeCallback));
            _feedbackPublisher = feedbackPublisher;
            _serializer = serializer ?? new JsonMessageSerializer();

            IsRunning = true;

            if (_node.Context.IsSimulated)
            {
                Console.WriteLine($"[RosActionServer] [SIM] Action server '{ActionName}' is running (simulation mode)");
            }
            else
            {
                Console.WriteLine($"[RosActionServer] Action server '{ActionName}' started on node '{_node.Name}'");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                IsRunning = false;
                Console.WriteLine($"[RosActionServer] Disposed action server for '{ActionName}'");
            }
        }
    }
}
