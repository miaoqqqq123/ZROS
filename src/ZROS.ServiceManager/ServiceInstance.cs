using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using ZROS.ServiceManager.Models;
using ZROS.Core.Logging;

namespace ZROS.ServiceManager
{
    /// <summary>
    /// Represents a single running instance of a managed service process.
    /// Handles process start/stop, monitoring process liveness, and collecting resource metrics.
    /// </summary>
    public class ServiceInstance : IDisposable
    {
        private Process? _process;
        private readonly ServiceDefinition _definition;
        private readonly ILogger<ServiceInstance> _logger;
        private readonly object _lock = new object();
        private bool _disposed;

        public string Name => _definition.Name;

        /// <summary>Current lifecycle state of this service instance.</summary>
        public ServiceState State { get; private set; } = ServiceState.Stopped;

        /// <summary>OS process ID, or -1 when not running.</summary>
        public int ProcessId => _process?.Id ?? -1;

        /// <summary>UTC timestamp of the last successful start.</summary>
        public DateTime StartTime { get; private set; } = DateTime.MinValue;

        /// <summary>Number of times this instance has been (re)started.</summary>
        public int RestartCount { get; private set; } = 0;

        /// <summary>True when the underlying OS process is alive.</summary>
        public bool IsAlive
        {
            get
            {
                lock (_lock)
                {
                    if (_process == null) return false;
                    try { return !_process.HasExited; }
                    catch { return false; }
                }
            }
        }

        public ServiceInstance(ServiceDefinition definition, ILogger<ServiceInstance>? logger = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<ServiceInstance>();
        }

        /// <summary>Starts the service process. Throws <see cref="InvalidOperationException"/> if already running.</summary>
        public void Start()
        {
            lock (_lock)
            {
                if (State == ServiceState.Running)
                    throw new InvalidOperationException($"Service '{Name}' is already running.");

                if (string.IsNullOrWhiteSpace(_definition.Executable))
                    throw new InvalidOperationException($"Service '{Name}' has no executable configured.");

                State = ServiceState.Starting;
                _logger.LogInformation("Starting service '{ServiceName}': {Executable}", Name, _definition.Executable);

                try
                {
                    var startInfo = BuildStartInfo();
                    _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                    _process.Exited += OnProcessExited;
                    _process.Start();

                    StartTime = DateTime.UtcNow;
                    State = ServiceState.Running;
                    _logger.LogInformation("Service '{ServiceName}' started with PID {Pid}", Name, _process.Id);
                }
                catch (Exception ex)
                {
                    State = ServiceState.Faulted;
                    _logger.LogError(ex, "Failed to start service '{ServiceName}'", Name);
                    throw;
                }
            }
        }

        /// <summary>Gracefully stops the service process.</summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (_process == null || _process.HasExited)
                {
                    State = ServiceState.Stopped;
                    return;
                }

                State = ServiceState.Stopping;
                _logger.LogInformation("Stopping service '{ServiceName}' (PID {Pid})", Name, _process.Id);
                try
                {
                    _process.CloseMainWindow();
                    if (!_process.WaitForExit(5000))
                    {
                        _process.Kill();
                        _logger.LogWarning("Killed service '{ServiceName}' after graceful stop timeout", Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping service '{ServiceName}'", Name);
                }
                finally
                {
                    State = ServiceState.Stopped;
                }
            }
        }

        /// <summary>Returns the working-set memory usage in bytes, or 0 if unavailable.</summary>
        public long GetMemoryUsage()
        {
            lock (_lock)
            {
                if (_process == null || _process.HasExited) return 0;
                try
                {
                    _process.Refresh();
                    return _process.WorkingSet64;
                }
                catch { return 0; }
            }
        }

        /// <summary>
        /// Performs a basic health check (process-alive).
        /// Sets State to Faulted if the process has exited unexpectedly.
        /// </summary>
        public void PerformHealthCheck()
        {
            lock (_lock)
            {
                if (State != ServiceState.Running) return;
                if (!IsAlive)
                {
                    State = ServiceState.Faulted;
                    _logger.LogWarning("Health check failed for '{ServiceName}': process is no longer alive", Name);
                }
            }
        }

        /// <summary>Increments the restart counter (called by the fault recovery engine).</summary>
        internal void IncrementRestartCount() => RestartCount++;

        private void OnProcessExited(object? sender, EventArgs e)
        {
            lock (_lock)
            {
                if (State == ServiceState.Stopping || State == ServiceState.Stopped)
                    return; // intentional stop

                int? exitCode = null;
                try { exitCode = _process?.ExitCode; } catch { /* process handle might be closed */ }
                _logger.LogWarning("Service '{ServiceName}' exited unexpectedly (exit code {ExitCode})", Name, exitCode);
                State = ServiceState.Faulted;
            }
        }

        private ProcessStartInfo BuildStartInfo()
        {
            var args = _definition.Arguments != null
                ? string.Join(" ", _definition.Arguments)
                : string.Empty;

            var startInfo = new ProcessStartInfo
            {
                FileName = _definition.Executable,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WorkingDirectory = _definition.WorkingDirectory
                    ?? System.IO.Path.GetDirectoryName(_definition.Executable)
                    ?? System.IO.Directory.GetCurrentDirectory()
            };

            if (_definition.Environment != null)
            {
                foreach (var kvp in _definition.Environment)
                    startInfo.Environment[kvp.Key] = kvp.Value;
            }

            return startInfo;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                try { Stop(); } catch { /* best-effort */ }
                _process?.Dispose();
            }
        }
    }
}
