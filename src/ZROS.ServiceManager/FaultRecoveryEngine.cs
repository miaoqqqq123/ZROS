using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZROS.ServiceManager.Models;
using ZROS.Core.Logging;

namespace ZROS.ServiceManager
{
    /// <summary>
    /// Monitors registered <see cref="ServiceInstance"/> objects and automatically restarts
    /// faulted services according to their <see cref="RestartPolicy"/>.
    /// Uses an exponential backoff strategy between restart attempts.
    /// </summary>
    public class FaultRecoveryEngine : IDisposable
    {
        private readonly Dictionary<string, ServiceInstance> _instances;
        private readonly ServiceRegistry _registry;
        private readonly EventBus _eventBus;
        private readonly ILogger<FaultRecoveryEngine> _logger;

        private CancellationTokenSource? _cts;
        private Task? _monitorTask;
        private bool _disposed;

        /// <summary>Interval between monitoring sweeps.</summary>
        public TimeSpan MonitorInterval { get; set; } = TimeSpan.FromSeconds(2);

        public FaultRecoveryEngine(
            Dictionary<string, ServiceInstance> instances,
            ServiceRegistry registry,
            EventBus eventBus,
            ILogger<FaultRecoveryEngine>? logger = null)
        {
            _instances = instances ?? throw new ArgumentNullException(nameof(instances));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<FaultRecoveryEngine>();
        }

        /// <summary>Starts the background monitoring loop.</summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(FaultRecoveryEngine));
            if (_monitorTask != null) return;

            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorServicesAsync(_cts.Token));
            _logger.LogInformation("FaultRecoveryEngine started (interval={Interval}s)", MonitorInterval.TotalSeconds);
        }

        /// <summary>Stops the background monitoring loop.</summary>
        public void Stop()
        {
            _cts?.Cancel();
            try { _monitorTask?.Wait(TimeSpan.FromSeconds(5)); } catch { /* best-effort */ }
            _monitorTask = null;
            _cts?.Dispose();
            _cts = null;
            _logger.LogInformation("FaultRecoveryEngine stopped");
        }

        private async Task MonitorServicesAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(MonitorInterval, ct).ConfigureAwait(false);
                    MonitorServices(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in fault recovery monitor");
                }
            }
        }

        private void MonitorServices(CancellationToken ct)
        {
            List<(string Name, ServiceInstance Instance)> faulted;
            lock (_instances)
            {
                faulted = new List<(string, ServiceInstance)>();
                foreach (var kvp in _instances)
                {
                    kvp.Value.PerformHealthCheck();
                    if (kvp.Value.State == ServiceState.Faulted)
                        faulted.Add((kvp.Key, kvp.Value));
                }
            }

            foreach (var (name, instance) in faulted)
            {
                if (ct.IsCancellationRequested) break;
                _ = ApplyRestartPolicyAsync(name, instance, ct);
            }
        }

        private async Task ApplyRestartPolicyAsync(string serviceName, ServiceInstance instance, CancellationToken ct)
        {
            var definition = _registry.GetService(serviceName);
            if (definition == null) return;

            var policy = definition.RestartPolicy;
            if (!policy.AutoRestart)
            {
                _logger.LogInformation("Auto-restart disabled for '{ServiceName}'", serviceName);
                return;
            }

            if (instance.RestartCount >= policy.MaxRetries)
            {
                _logger.LogWarning("Service '{ServiceName}' exceeded max restart attempts ({Max})", serviceName, policy.MaxRetries);
                _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.RestartLimitReached,
                    $"Exceeded max retries ({policy.MaxRetries})"));
                return;
            }

            int delayMs = CalculateBackoffDelay(instance.RestartCount, policy);
            _logger.LogInformation("Restarting '{ServiceName}' in {Delay}ms (attempt {Attempt}/{Max})",
                serviceName, delayMs, instance.RestartCount + 1, policy.MaxRetries);

            _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.Restarting,
                $"Attempt {instance.RestartCount + 1}/{policy.MaxRetries} in {delayMs}ms"));

            await Task.Delay(delayMs, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) return;

            try
            {
                instance.IncrementRestartCount();
                instance.Start();
                _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.Restarted));
                _logger.LogInformation("Service '{ServiceName}' restarted successfully", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart service '{ServiceName}'", serviceName);
                _eventBus.Publish(new ServiceEvent(serviceName, ServiceEventType.Faulted, ex.Message));
            }
        }

        public int CalculateBackoffDelay(int retryCount, RestartPolicy policy)
        {
            if (retryCount <= 0) return policy.InitialDelayMs;
            var delay = policy.InitialDelayMs * Math.Pow(policy.BackoffMultiplier, retryCount);
            return (int)Math.Min(delay, policy.MaxDelayMs);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Stop();
            }
        }
    }
}
