using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZROS.ServiceManager.Models;
using ZROS.Core.Logging;

namespace ZROS.ServiceManager
{
    /// <summary>
    /// The central orchestrator that manages the lifecycle of all registered services.
    /// Integrates the <see cref="ServiceRegistry"/>, <see cref="FaultRecoveryEngine"/>,
    /// <see cref="DependencyResolver"/>, and <see cref="EventBus"/> subsystems.
    /// </summary>
    public class ServiceManager : IDisposable
    {
        private readonly ServiceRegistry _registry;
        private readonly DependencyResolver _resolver;
        private readonly EventBus _eventBus;
        private readonly FaultRecoveryEngine _faultRecovery;
        private readonly Dictionary<string, ServiceInstance> _instances;
        private readonly ILogger<ServiceManager> _logger;
        private readonly object _instanceLock = new object();
        private bool _disposed;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>Creates a new ServiceManager with default components.</summary>
        public ServiceManager(ILogger<ServiceManager>? logger = null)
        {
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<ServiceManager>();
            _registry = new ServiceRegistry();
            _resolver = new DependencyResolver();
            _eventBus = new EventBus();
            _instances = new Dictionary<string, ServiceInstance>(StringComparer.OrdinalIgnoreCase);
            _faultRecovery = new FaultRecoveryEngine(_instances, _registry, _eventBus);
            _faultRecovery.Start();
            _logger.LogInformation("ServiceManager initialized");
        }

        // ──────────────────────────────────────────────────────────────
        // Service Operations
        // ──────────────────────────────────────────────────────────────

        /// <summary>Registers a service definition with the manager.</summary>
        public void RegisterService(ServiceDefinition definition)
        {
            _registry.RegisterService(definition);
            _eventBus.Publish(new ServiceEvent(definition.Name, ServiceEventType.Registered));
        }

        /// <summary>Removes a service from the registry. The service must be stopped first.</summary>
        public void UnregisterService(string serviceName)
        {
            EnsureRunning();
            lock (_instanceLock)
            {
                if (_instances.TryGetValue(serviceName, out var inst) && inst.State == ServiceState.Running)
                    throw new InvalidOperationException($"Cannot unregister running service '{serviceName}'. Stop it first.");
            }
            _registry.UnregisterService(serviceName);
            lock (_instanceLock) { _instances.Remove(serviceName); }
            _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.Unregistered));
        }

        /// <summary>Starts a single service by name, respecting its dependency chain.</summary>
        public async Task StartServiceAsync(string serviceName)
        {
            EnsureRunning();
            var definition = GetDefinitionOrThrow(serviceName);
            var allDefs = _registry.GetAllServices().ToList();

            // resolve and start dependencies first
            var deps = _resolver.ResolveDependencies(serviceName, allDefs);
            foreach (var dep in deps)
            {
                var status = GetServiceStatus(dep);
                if (status.State != ServiceState.Running)
                    await StartSingleServiceAsync(dep).ConfigureAwait(false);
            }

            await StartSingleServiceAsync(serviceName).ConfigureAwait(false);
        }

        /// <summary>Stops a single service by name.</summary>
        public Task StopServiceAsync(string serviceName)
        {
            EnsureRunning();
            GetDefinitionOrThrow(serviceName);
            return Task.Run(() => StopSingleService(serviceName));
        }

        /// <summary>Stops then starts a service.</summary>
        public async Task RestartServiceAsync(string serviceName)
        {
            await StopServiceAsync(serviceName).ConfigureAwait(false);
            await StartServiceAsync(serviceName).ConfigureAwait(false);
        }

        /// <summary>Starts all registered services that have AutoStart enabled, in dependency order.</summary>
        public async Task StartAllAsync()
        {
            EnsureRunning();
            var allDefs = _registry.GetAllServices().ToList();
            if (_resolver.HasCyclicDependency(allDefs, out var cycle))
                throw new InvalidOperationException($"Cyclic dependency detected: {cycle}");

            var names = allDefs.Where(d => d.AutoStart && d.StartType != ServiceStartType.Disabled)
                               .Select(d => d.Name);
            var ordered = _resolver.GetTopologicalOrder(names, allDefs);

            foreach (var name in ordered)
            {
                var status = GetServiceStatus(name);
                if (status.State != ServiceState.Running)
                    await StartSingleServiceAsync(name).ConfigureAwait(false);
            }
        }

        /// <summary>Stops all currently running services in reverse dependency order.</summary>
        public async Task StopAllAsync()
        {
            EnsureRunning();
            var allDefs = _registry.GetAllServices().ToList();
            var names = allDefs.Select(d => d.Name);
            var ordered = _resolver.GetTopologicalOrder(names, allDefs);
            ordered.Reverse(); // stop in reverse of start order

            foreach (var name in ordered)
            {
                var status = GetServiceStatus(name);
                if (status.State == ServiceState.Running)
                    await Task.Run(() => StopSingleService(name)).ConfigureAwait(false);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Status Queries
        // ──────────────────────────────────────────────────────────────

        /// <summary>Returns a status snapshot for the given service.</summary>
        public ServiceStatus GetServiceStatus(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("Name cannot be empty.", nameof(serviceName));
            lock (_instanceLock)
            {
                if (_instances.TryGetValue(serviceName, out var inst))
                {
                    return new ServiceStatus
                    {
                        Name = serviceName,
                        State = inst.State,
                        ProcessId = inst.ProcessId,
                        StartTime = inst.StartTime,
                        RestartCount = inst.RestartCount,
                        MemoryUsageBytes = inst.GetMemoryUsage()
                    };
                }
            }
            return new ServiceStatus { Name = serviceName, State = ServiceState.Stopped };
        }

        /// <summary>Returns a status snapshot for every registered service.</summary>
        public Dictionary<string, ServiceStatus> GetAllServiceStatus()
        {
            var result = new Dictionary<string, ServiceStatus>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in _registry.GetAllServices())
                result[def.Name] = GetServiceStatus(def.Name);
            return result;
        }

        /// <summary>Returns an observable stream of service lifecycle events.</summary>
        public IObservable<ServiceEvent> GetServiceEvents() => _eventBus.GetEvents();

        // ──────────────────────────────────────────────────────────────
        // Configuration Management
        // ──────────────────────────────────────────────────────────────

        /// <summary>Loads service definitions from a JSON or YAML file.</summary>
        public void LoadConfiguration(string configPath)
        {
            _registry.LoadFromFile(configPath);
            _logger.LogInformation("Configuration loaded from '{Path}'", configPath);
        }

        /// <summary>Saves current service definitions to a JSON file.</summary>
        public void SaveConfiguration(string configPath)
        {
            _registry.SaveToFile(configPath);
            _logger.LogInformation("Configuration saved to '{Path}'", configPath);
        }

        /// <summary>Loads a recipe from a JSON file and registers all its services.</summary>
        public Recipe LoadRecipe(string recipePath)
        {
            if (!File.Exists(recipePath)) throw new FileNotFoundException("Recipe file not found.", recipePath);
            var json = File.ReadAllText(recipePath);
            var recipe = JsonSerializer.Deserialize<Recipe>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize recipe.");

            foreach (var svc in recipe.Services)
                _registry.RegisterService(svc);

            _logger.LogInformation("Loaded recipe '{RecipeName}' with {Count} service(s)", recipe.Name, recipe.Services.Count);
            return recipe;
        }

        /// <summary>Saves the current service configuration as a recipe to a JSON file.</summary>
        public void SaveRecipe(Recipe recipe, string recipePath)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            recipe.LastModified = DateTime.UtcNow;

            var directory = Path.GetDirectoryName(recipePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(recipe, _jsonOptions);
            File.WriteAllText(recipePath, json);
            _logger.LogInformation("Recipe '{RecipeName}' saved to '{Path}'", recipe.Name, recipePath);
        }

        // ──────────────────────────────────────────────────────────────
        // Internal helpers
        // ──────────────────────────────────────────────────────────────

        private async Task StartSingleServiceAsync(string serviceName)
        {
            ServiceInstance instance;
            lock (_instanceLock)
            {
                if (!_instances.TryGetValue(serviceName, out instance!))
                {
                    var def = GetDefinitionOrThrow(serviceName);
                    instance = new ServiceInstance(def);
                    _instances[serviceName] = instance;
                }

                if (instance.State == ServiceState.Running) return;
            }

            _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.Starting));
            await Task.Run(() => instance.Start()).ConfigureAwait(false);
            _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.Started));
        }

        private void StopSingleService(string serviceName)
        {
            ServiceInstance? instance;
            lock (_instanceLock)
            {
                _instances.TryGetValue(serviceName, out instance);
            }
            if (instance == null) return;

            _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.Stopping));
            instance.Stop();
            _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.Stopped));
        }

        private ServiceDefinition GetDefinitionOrThrow(string serviceName)
        {
            var def = _registry.GetService(serviceName);
            if (def == null)
                throw new InvalidOperationException($"Service '{serviceName}' is not registered.");
            return def;
        }

        private void EnsureRunning()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceManager));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _faultRecovery.Dispose();
                lock (_instanceLock)
                {
                    foreach (var inst in _instances.Values)
                    {
                        try { inst.Dispose(); } catch { /* best-effort */ }
                    }
                    _instances.Clear();
                }
                _eventBus.Dispose();
                _logger.LogInformation("ServiceManager disposed");
            }
        }
    }
}
