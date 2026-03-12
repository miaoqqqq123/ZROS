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
                // zenoh-c v1.x: config must be created explicitly before z_open.
                // z_open(session*, moved_config*, open_options*) – 3 arguments.
                ZenohNative.z_config_default(out var config);
                int result = ZenohNative.z_open(out _session, ref config, IntPtr.Zero);
                if (result == ZenohErrorCodes.Z_OK)
                {
                    _sessionOpen = true;
                    IsSimulated = false;
                    _logger.Info("Zenoh session opened successfully.");
                }
                else
                {
                    // z_open failed: config ownership was NOT transferred, drop it.
                    ZenohNative.z_config_drop(ref config);
                    IsSimulated = true;
                    _logger.Warn("Zenoh session failed to open (code={ErrorCode}: {Msg}), running in simulation mode.",
                        result, ZenohErrorCodes.GetErrorString(result));
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
                        // zenoh-c v1.x: z_session_drop is what z_drop(z_move(session)) calls.
                        ZenohNative.z_session_drop(ref _session);
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
