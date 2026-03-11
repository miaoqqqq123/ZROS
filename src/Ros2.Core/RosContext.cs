using System;
using System.Runtime.InteropServices;
using Zenoh.Native;

namespace Ros2.Core
{
    public class RosContext : IDisposable
    {
        private z_owned_session_t _session;
        private bool _disposed;
        private bool _sessionOpen;

        public bool IsOpen => !_disposed && (_sessionOpen || IsSimulated);
        public bool IsSimulated { get; private set; }

        public RosContext(string? configPath = null)
        {
            Console.WriteLine("[RosContext] Initializing Zenoh session...");
            try
            {
                int result = ZenohNative.z_open(out _session, IntPtr.Zero);
                if (result == ZenohErrorCodes.Z_OK)
                {
                    _sessionOpen = true;
                    IsSimulated = false;
                    Console.WriteLine("[RosContext] Zenoh session opened successfully.");
                }
                else
                {
                    IsSimulated = true;
                    Console.WriteLine($"[RosContext] Zenoh session failed to open (code={result}), running in simulation mode.");
                }
            }
            catch (DllNotFoundException ex)
            {
                IsSimulated = true;
                Console.WriteLine($"[RosContext] Native zenoh library not found ({ex.Message}), running in simulation mode.");
            }
            catch (EntryPointNotFoundException ex)
            {
                IsSimulated = true;
                Console.WriteLine($"[RosContext] Zenoh entry point not found ({ex.Message}), running in simulation mode.");
            }
            catch (BadImageFormatException ex)
            {
                IsSimulated = true;
                Console.WriteLine($"[RosContext] Bad image format for zenoh library ({ex.Message}), running in simulation mode.");
            }
        }

        public static RosContext Create(string? configPath = null)
        {
            return new RosContext(configPath);
        }

        public RosNode CreateNode(string name, RosNodeOptions? options = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RosContext));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Node name cannot be empty.", nameof(name));
            return new RosNode(name, this, options);
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
                        Console.WriteLine("[RosContext] Zenoh session closed.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RosContext] Error closing session: {ex.Message}");
                    }
                }
                _sessionOpen = false;
            }
        }
    }
}
