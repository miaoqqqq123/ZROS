using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ZROS.ServiceManager.Models
{
    /// <summary>
    /// A Recipe is a named, versioned snapshot of a set of service definitions that can be
    /// saved to disk and loaded later to recreate the same service topology.
    /// </summary>
    public class Recipe
    {
        /// <summary>Unique identifier (UUID or short slug).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Human-readable name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Semver version string.</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>UTC timestamp when this recipe was first created.</summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent modification.</summary>
        [JsonPropertyName("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>Service definitions included in this recipe.</summary>
        public List<ServiceDefinition> Services { get; set; } = new List<ServiceDefinition>();
    }
}
