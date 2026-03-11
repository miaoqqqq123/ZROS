namespace ZROS.ServiceManager.Models
{
    /// <summary>Determines when a service should be started.</summary>
    public enum ServiceStartType
    {
        /// <summary>Service starts automatically with the system.</summary>
        Automatic,
        /// <summary>Service must be started manually.</summary>
        Manual,
        /// <summary>Service is disabled and cannot be started.</summary>
        Disabled
    }
}
