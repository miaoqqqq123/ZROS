using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace ZROS.ServiceManager.Models
{
    /// <summary>Describes a managed service and its runtime configuration.</summary>
    public class ServiceDefinition
    {
        /// <summary>Unique identifier for the service (used internally).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Short machine-readable name (e.g. "logger_service").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Human-readable display name.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Optional description of what the service does.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Path to the service executable.</summary>
        public string Executable { get; set; } = string.Empty;

        /// <summary>Command-line arguments passed to the executable on startup.</summary>
        [JsonPropertyName("arguments")]
        public string[] Arguments { get; set; } = System.Array.Empty<string>();

        /// <summary>Names of services that must be running before this service starts.</summary>
        [JsonPropertyName("depends_on")]
        public string[] DependsOn { get; set; } = System.Array.Empty<string>();

        /// <summary>Whether the service starts automatically.</summary>
        [JsonPropertyName("auto_start")]
        public bool AutoStart { get; set; } = true;

        /// <summary>Start-type determining when the service starts.</summary>
        [JsonPropertyName("start_type")]
        public ServiceStartType StartType { get; set; } = ServiceStartType.Automatic;

        /// <summary>Restart behaviour on unexpected exit.</summary>
        [JsonPropertyName("restart_policy")]
        public RestartPolicy RestartPolicy { get; set; } = new RestartPolicy();

        /// <summary>Optional health-check configuration.</summary>
        [JsonPropertyName("health_check")]
        public HealthCheckConfig HealthCheck { get; set; } = new HealthCheckConfig();

        /// <summary>Optional resource usage limits.</summary>
        [JsonPropertyName("resource_limits")]
        public ResourceLimits ResourceLimits { get; set; } = new ResourceLimits();

        /// <summary>Additional environment variables injected into the process.</summary>
        public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();

        /// <summary>Working directory for the process. Null uses the executable's directory.</summary>
        [JsonPropertyName("working_directory")]
        public string? WorkingDirectory { get; set; }
    }
}
