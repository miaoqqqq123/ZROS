namespace ZROS.ServiceManager.Models
{
    /// <summary>Configures the automatic restart behaviour for a service.</summary>
    public class RestartPolicy
    {
        /// <summary>Whether to automatically restart the service when it exits unexpectedly.</summary>
        public bool AutoRestart { get; set; } = true;

        /// <summary>Maximum number of consecutive restart attempts before the service is marked as Failed.</summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>Delay in milliseconds between the first restart attempt.</summary>
        public int InitialDelayMs { get; set; } = 1000;

        /// <summary>Multiplier applied to the delay on each subsequent retry (exponential backoff).</summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>Maximum delay in milliseconds between retry attempts.</summary>
        public int MaxDelayMs { get; set; } = 30000;

        /// <summary>Window in seconds within which restart counts are tracked. Restarts outside this window reset the count.</summary>
        public int RestartWindowSeconds { get; set; } = 60;
    }
}
