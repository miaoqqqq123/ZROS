using System;

namespace ZROS.ServiceManager.Models
{
    /// <summary>Runtime status snapshot of a managed service.</summary>
    public class ServiceStatus
    {
        /// <summary>Service name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Current lifecycle state.</summary>
        public ServiceState State { get; set; } = ServiceState.Stopped;

        /// <summary>OS process ID, or -1 when not running.</summary>
        public int ProcessId { get; set; } = -1;

        /// <summary>UTC timestamp when the service last started.</summary>
        public DateTime StartTime { get; set; } = DateTime.MinValue;

        /// <summary>Total number of times the service has been restarted.</summary>
        public int RestartCount { get; set; } = 0;

        /// <summary>Memory usage in bytes of the service process.</summary>
        public long MemoryUsageBytes { get; set; } = 0;

        /// <summary>CPU usage percentage (0–100).</summary>
        public double CpuUsagePercent { get; set; } = 0;

        /// <summary>How long the service has been running since the last start.</summary>
        public TimeSpan Uptime => State == ServiceState.Running && StartTime != DateTime.MinValue
            ? DateTime.UtcNow - StartTime
            : TimeSpan.Zero;
    }
}
