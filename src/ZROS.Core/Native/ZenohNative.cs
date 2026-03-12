using System;
using System.Runtime.InteropServices;

namespace ZROS.Core.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_session_t
    {
        public IntPtr _0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_publisher_t
    {
        public IntPtr _0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_subscriber_t
    {
        public IntPtr _0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_queryable_t
    {
        public IntPtr _0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_bytes_t
    {
        public UIntPtr len;
        public IntPtr start;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_keyexpr_t
    {
        public UIntPtr id;
        public IntPtr suffix;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_keyexpr_t
    {
        public IntPtr _0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_query_t
    {
        public IntPtr _0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_reply_t
    {
        public IntPtr _0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct z_sample_t
    {
        public z_keyexpr_t keyexpr;
        public z_bytes_t payload;
        public int encoding;
        public ulong timestamp;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void z_data_handler_t(IntPtr sample, IntPtr arg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void z_queryable_handler_t(IntPtr query, IntPtr arg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void z_reply_handler_t(IntPtr reply, IntPtr arg);

    public static class ZenohErrorCodes
    {
        public const int Z_OK = 0;
        public const int Z_EINVAL = -1;
        public const int Z_EPARSE = -2;
        public const int Z_ENOMEM = -3;
        public const int Z_ECONNREFUSED = -4;
        public const int Z_ESESSION_CLOSED = -5;

        public static string GetErrorString(int code) => code switch
        {
            Z_OK => "OK",
            Z_EINVAL => "Invalid argument",
            Z_EPARSE => "Parse error",
            Z_ENOMEM => "Out of memory",
            Z_ECONNREFUSED => "Connection refused",
            Z_ESESSION_CLOSED => "Session closed",
            _ => $"Unknown error ({code})"
        };
    }

    /// <summary>
    /// P/Invoke layer for zenoh-c native library.
    /// All methods will throw DllNotFoundException if the native library is not installed.
    /// Consumers should catch DllNotFoundException and fall back to simulation mode.
    /// </summary>
    public static class ZenohNative
    {
        private const string LibName = "zenohc";

        [DllImport(LibName, EntryPoint = "z_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_open(out z_owned_session_t session, IntPtr config);

        [DllImport(LibName, EntryPoint = "z_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_close(ref z_owned_session_t session);

        [DllImport(LibName, EntryPoint = "z_declare_publisher", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_publisher(
            out z_owned_publisher_t publisher,
            ref z_owned_session_t session,
            z_keyexpr_t keyexpr,
            IntPtr options);

        [DllImport(LibName, EntryPoint = "z_undeclare_publisher", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_undeclare_publisher(ref z_owned_publisher_t publisher);

        [DllImport(LibName, EntryPoint = "z_publisher_put", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_publisher_put(
            ref z_owned_publisher_t publisher,
            IntPtr payload,
            UIntPtr len,
            IntPtr options);

        [DllImport(LibName, EntryPoint = "z_declare_subscriber", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_subscriber(
            out z_owned_subscriber_t subscriber,
            ref z_owned_session_t session,
            z_keyexpr_t keyexpr,
            z_data_handler_t callback,
            IntPtr arg);

        [DllImport(LibName, EntryPoint = "z_undeclare_subscriber", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_undeclare_subscriber(ref z_owned_subscriber_t subscriber);

        [DllImport(LibName, EntryPoint = "z_declare_queryable", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_queryable(
            out z_owned_queryable_t queryable,
            ref z_owned_session_t session,
            z_keyexpr_t keyexpr,
            z_queryable_handler_t callback,
            IntPtr arg);

        [DllImport(LibName, EntryPoint = "z_undeclare_queryable", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_undeclare_queryable(ref z_owned_queryable_t queryable);

        [DllImport(LibName, EntryPoint = "z_query_reply", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_query_reply(
            IntPtr query,
            z_keyexpr_t keyexpr,
            IntPtr payload,
            UIntPtr len,
            IntPtr options);

        [DllImport(LibName, EntryPoint = "z_get", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_get(
            ref z_owned_session_t session,
            z_keyexpr_t keyexpr,
            IntPtr parameters,
            z_reply_handler_t callback,
            IntPtr arg,
            IntPtr options);

        /// <summary>
        /// Attempts to load the native zenoh library to verify availability.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public static bool TryLoad()
        {
            try
            {
                z_owned_session_t session;
                int result = z_open(out session, IntPtr.Zero);
                if (result == 0)
                    z_close(ref session);
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
