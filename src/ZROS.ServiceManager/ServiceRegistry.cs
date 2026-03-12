using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ZROS.ServiceManager.Models;
using ZROS.Core.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ZROS.ServiceManager
{
    /// <summary>
    /// Stores and retrieves <see cref="ServiceDefinition"/> entries.
    /// Supports loading from and saving to JSON and YAML configuration files.
    /// </summary>
    public class ServiceRegistry
    {
        private readonly Dictionary<string, ServiceDefinition> _services = new Dictionary<string, ServiceDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<ServiceRegistry> _logger;
        private readonly object _lock = new object();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public ServiceRegistry(ILogger<ServiceRegistry>? logger = null)
        {
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<ServiceRegistry>();
        }

        /// <summary>Returns the definition for the given service name, or null if not found.</summary>
        public ServiceDefinition? GetService(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
            lock (_lock)
            {
                _services.TryGetValue(name, out var def);
                return def;
            }
        }

        /// <summary>Returns all registered service definitions.</summary>
        public IEnumerable<ServiceDefinition> GetAllServices()
        {
            lock (_lock)
            {
                return new List<ServiceDefinition>(_services.Values);
            }
        }

        /// <summary>Registers a new service definition, replacing any existing entry with the same name.</summary>
        public void RegisterService(ServiceDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.Name))
                throw new ArgumentException("ServiceDefinition.Name cannot be empty.");

            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(definition.Id))
                    definition.Id = Guid.NewGuid().ToString();
                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                    definition.DisplayName = definition.Name;

                _services[definition.Name] = definition;
                _logger.LogInformation("Registered service '{ServiceName}'", definition.Name);
            }
        }

        /// <summary>Removes the service definition with the given name. Returns true if found.</summary>
        public bool UnregisterService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("Name cannot be empty.", nameof(serviceName));
            lock (_lock)
            {
                if (_services.Remove(serviceName))
                {
                    _logger.LogInformation("Unregistered service '{ServiceName}'", serviceName);
                    return true;
                }
                return false;
            }
        }

        /// <summary>Returns true if a service with the given name is registered.</summary>
        public bool Contains(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) return false;
            lock (_lock)
            {
                return _services.ContainsKey(serviceName);
            }
        }

        /// <summary>Loads service definitions from a JSON or YAML file, replacing all existing entries.</summary>
        public void LoadFromFile(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException("Config path cannot be empty.", nameof(configPath));
            if (!File.Exists(configPath)) throw new FileNotFoundException("Configuration file not found.", configPath);

            var ext = Path.GetExtension(configPath).ToLowerInvariant();
            List<ServiceDefinition> definitions;

            if (ext == ".yaml" || ext == ".yml")
            {
                definitions = LoadFromYaml(configPath);
            }
            else
            {
                definitions = LoadFromJson(configPath);
            }

            lock (_lock)
            {
                _services.Clear();
                foreach (var def in definitions)
                {
                    if (string.IsNullOrWhiteSpace(def.Id)) def.Id = Guid.NewGuid().ToString();
                    if (string.IsNullOrWhiteSpace(def.DisplayName)) def.DisplayName = def.Name;
                    _services[def.Name] = def;
                }
            }
            _logger.LogInformation("Loaded {Count} service(s) from '{Path}'", definitions.Count, configPath);
        }

        /// <summary>Saves all current service definitions to a JSON file.</summary>
        public void SaveToFile(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException("Config path cannot be empty.", nameof(configPath));

            List<ServiceDefinition> snapshot;
            lock (_lock)
            {
                snapshot = new List<ServiceDefinition>(_services.Values);
            }

            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            File.WriteAllText(configPath, json);
            _logger.LogInformation("Saved {Count} service(s) to '{Path}'", snapshot.Count, configPath);
        }

        private List<ServiceDefinition> LoadFromJson(string path)
        {
            var json = File.ReadAllText(path);
            var result = JsonSerializer.Deserialize<List<ServiceDefinition>>(json, _jsonOptions);
            return result ?? new List<ServiceDefinition>();
        }

        private List<ServiceDefinition> LoadFromYaml(string path)
        {
            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var result = deserializer.Deserialize<List<ServiceDefinition>>(yaml);
            return result ?? new List<ServiceDefinition>();
        }
    }
}
