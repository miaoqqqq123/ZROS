using System;

namespace ZROS.ServiceManager.Models
{
    /// <summary>Types of events emitted by the service manager event bus.</summary>
    public enum ServiceEventType
    {
        Starting,
        Started,
        Stopping,
        Stopped,
        Faulted,
        Restarting,
        Restarted,
        RestartLimitReached,
        HealthCheckPassed,
        HealthCheckFailed,
        Registered,
        Unregistered
    }

    /// <summary>Immutable event published on the service event bus.</summary>
    public class ServiceEvent
    {
        public string ServiceName { get; }
        public ServiceEventType EventType { get; }
        public DateTime Timestamp { get; }
        public string? Message { get; }

        public ServiceEvent(string serviceName, ServiceEventType eventType, string? message = null)
        {
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            EventType = eventType;
            Timestamp = DateTime.UtcNow;
            Message = message;
        }

        public override string ToString() =>
            $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {ServiceName} -> {EventType}" +
            (Message != null ? $": {Message}" : string.Empty);
    }
}
