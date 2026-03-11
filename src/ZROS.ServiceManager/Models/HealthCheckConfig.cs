namespace ZROS.ServiceManager.Models
{
    /// <summary>Configures the health-check probe for a service.</summary>
    public class HealthCheckConfig
    {
        /// <summary>Whether health checks are enabled for this service.</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>Interval in seconds between health check executions.</summary>
        public int IntervalSeconds { get; set; } = 30;

        /// <summary>Number of consecutive failures before the service is considered unhealthy.</summary>
        public int FailureThreshold { get; set; } = 3;

        /// <summary>Timeout in seconds for a single health check execution.</summary>
        public int TimeoutSeconds { get; set; } = 5;

        /// <summary>HTTP endpoint to GET for HTTP-based health checks. Null uses process-alive check.</summary>
        public string? HttpEndpoint { get; set; }
    }
}
