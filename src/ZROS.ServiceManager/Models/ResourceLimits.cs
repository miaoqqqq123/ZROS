namespace ZROS.ServiceManager.Models
{
    /// <summary>Defines optional resource usage limits for a service process.</summary>
    public class ResourceLimits
    {
        /// <summary>Maximum memory allowed in megabytes. 0 means unlimited.</summary>
        public long MaxMemoryMb { get; set; } = 0;

        /// <summary>Maximum CPU usage percentage (0–100). 0 means unlimited.</summary>
        public double MaxCpuPercent { get; set; } = 0;
    }
}
