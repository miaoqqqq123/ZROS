using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZROS.Core.Native;
using ZROS.Core.Logging;

namespace ZROS.Core
{
    public class RosContext : IDisposable
    {
        private z_owned_session_t _session;
        private bool _disposed;
        private bool _sessionOpen;
        private readonly ILogger<RosContext> _logger;

        public bool IsOpen => !_disposed && (_sessionOpen || IsSimulated);
        public bool IsSimulated { get; private set; }

        public RosContext(string? configPath = null, ILogger<RosContext>? logger = null)
        {
            _logger = logger ?? ZrosLoggerFactory.CreateLogger<RosContext>();
            _logger.Info("Initializing Zenoh session...");
            try
            {
                int result = ZenohNative.z_open(out _session, IntPtr.Zero);
                if (result == ZenohErrorCodes.Z_OK)
                {
                    _sessionOpen = true;
                    IsSimulated = false;
                    _logger.Info("Zenoh session opened successfully.");
                }
                else
                {
                    IsSimulated = true;
                    _logger.Warn("Zenoh session failed to open (code={ErrorCode}), running in simulation mode.", result);
                }
            }
            catch (DllNotFoundException ex)
            {
                IsSimulated = true;
                _logger.Warn("Native zenoh library not found ({Message}), running in simulation mode.", ex.Message);
            }
            catch (EntryPointNotFoundException ex)
            {
                IsSimulated = true;
                _logger.Warn("Zenoh entry point not found ({Message}), running in simulation mode.", ex.Message);
            }
            catch (BadImageFormatException ex)
            {
                IsSimulated = true;
                _logger.Warn("Bad image format for zenoh library ({Message}), running in simulation mode.", ex.Message);
            }
        }

        public static RosContext Create(string? configPath = null, ILogger<RosContext>? logger = null)
        {
            return new RosContext(configPath, logger);
        }

        public RosNode CreateNode(string name, RosNodeOptions? options = null, ILogger<RosNode>? logger = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosContext));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Node name cannot be empty.", nameof(name));
            return new RosNode(name, this, options, logger);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_sessionOpen && !IsSimulated)
                {
                    try
                    {
                        ZenohNative.z_close(ref _session);
                        _logger.Info("Zenoh session closed.");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error closing Zenoh session.");
                    }
                }
                _sessionOpen = false;
            }
        }
    }
}
