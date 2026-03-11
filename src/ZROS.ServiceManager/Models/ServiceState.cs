namespace ZROS.ServiceManager.Models
{
    /// <summary>Represents the lifecycle state of a managed service.</summary>
    public enum ServiceState
    {
        /// <summary>Service has not been started.</summary>
        Stopped,
        /// <summary>Service is in the process of starting.</summary>
        Starting,
        /// <summary>Service is running normally.</summary>
        Running,
        /// <summary>Service is in the process of stopping.</summary>
        Stopping,
        /// <summary>Service encountered a fault and is being recovered.</summary>
        Faulted,
        /// <summary>Service exceeded max restart retries and will not restart automatically.</summary>
        Failed
    }
}
