using System;
using System.Runtime.InteropServices;

namespace ZROS.Core.Native
{
    // ====================================================================
    // Opaque struct definitions for zenoh-c v1.x ABI (Windows x86_64 MSVC)
    //
    // IMPORTANT – struct sizes MUST match the actual zenoh-c headers.
    // Sizes shown here are based on zenoh-c 1.7.2 x86_64-pc-windows-msvc.
    // Verify with: Marshal.SizeOf<T>() == expected value from zenoh_opaque.h
    //
    // API reference: https://zenoh-c.readthedocs.io/en/stable/
    // Migration guide: https://zenoh.io/docs/migration_1.0/c_pico/
    // ====================================================================

    /// <summary>
    /// Owned session handle. 8 bytes (opaque pointer to Rust session Arc).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.SessionBytes)]
    public struct z_owned_session_t { }

    /// <summary>
    /// Owned configuration handle. 8 bytes (opaque pointer to Rust Config).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.ConfigBytes)]
    public struct z_owned_config_t { }

    /// <summary>
    /// Owned publisher. 112 bytes (inline Rust publisher struct).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.PublisherBytes)]
    public struct z_owned_publisher_t { }

    /// <summary>
    /// Owned subscriber. 8 bytes (opaque subscriber handle).
    /// NOTE: Verify exact size from zenoh_opaque.h for your zenoh-c version.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.SubscriberBytes)]
    public struct z_owned_subscriber_t { }

    /// <summary>
    /// Owned queryable. 8 bytes (opaque queryable handle).
    /// NOTE: Verify exact size from zenoh_opaque.h for your zenoh-c version.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.QueryableBytes)]
    public struct z_owned_queryable_t { }

    /// <summary>
    /// Non-owning (borrowed) view of a key expression string.
    /// 32 bytes (generous estimate – verify against zenoh_opaque.h).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.ViewKeyexprBytes)]
    public struct z_view_keyexpr_t { }

    /// <summary>
    /// Owned bytes payload.
    /// 48 bytes (generous estimate – verify against zenoh_opaque.h).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.OwnedBytesBytes)]
    public struct z_owned_bytes_t { }

    /// <summary>
    /// Sample callback closure: context pointer + call function pointer + drop function pointer.
    /// 24 bytes (3 × 8 bytes on x64).
    /// z_closure(&cb, data_handler, NULL, NULL) fills these three fields.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_closure_sample_t
    {
        /// <summary>User-supplied opaque context pointer passed to <see cref="Call"/>.</summary>
        public IntPtr Context;
        /// <summary>Callback invoked for each received sample (z_loaned_sample_t*, void*).</summary>
        public IntPtr Call;
        /// <summary>Called when the closure is dropped to free <see cref="Context"/>.</summary>
        public IntPtr Drop;
    }

    /// <summary>
    /// Query callback closure (used with z_declare_queryable).
    /// 24 bytes (3 × 8 bytes on x64).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_closure_query_t
    {
        public IntPtr Context;
        public IntPtr Call;
        public IntPtr Drop;
    }

    /// <summary>
    /// Expected ABI sizes for zenoh-c v1.7.2 x86_64-pc-windows-msvc.
    /// Used both by struct definitions above and by the size-verification tests.
    ///
    /// Where only the value from the official header is known it is marked "confirmed";
    /// other values are reasonable estimates and MUST be verified against the actual
    /// zenoh_opaque.h for your specific zenoh-c build.
    ///
    /// To validate estimated sizes:
    ///   1. Open zenoh-c-&lt;version&gt;-x86_64-pc-windows-msvc-standalone/include/zenoh_opaque.h
    ///      and check the byte-array field of each z_owned_* struct.
    ///   2. Run the struct-size assertions:
    ///        dotnet test tests/ZROS.Tests --filter "Category=ZenohInterop"
    ///   3. See docs/interop-audit.md §2.4 for the full size table and update instructions.
    /// </summary>
    public static class ZenohAbiSizes
    {
        public const int SessionBytes     = 8;   // confirmed: z_owned_session_t   from zenoh-c 1.7.2 headers
        public const int ConfigBytes      = 8;   // estimated: z_owned_config_t     (single pointer)
        public const int PublisherBytes   = 112; // confirmed: z_owned_publisher_t  from zenoh-c 1.7.2 headers
        public const int SubscriberBytes  = 8;   // estimated: z_owned_subscriber_t (single pointer/handle)
        public const int QueryableBytes   = 8;   // estimated: z_owned_queryable_t  (single pointer/handle)
        public const int ViewKeyexprBytes = 32;  // estimated: z_view_keyexpr_t     (generous upper bound)
        public const int OwnedBytesBytes  = 48;  // estimated: z_owned_bytes_t      (generous upper bound)
        public const int ClosureSampleBytes = 24; // concrete:  3 pointers × 8 bytes
    }

    /// <summary>
    /// Error codes from zenoh-c v1.x zenoh_concrete.h.
    /// NOTE: these differ from v0.x (e.g. Z_ESESSION_CLOSED is now -8, not -5).
    /// </summary>
    public static class ZenohErrorCodes
    {
        public const int Z_OK           = 0;
        public const int Z_EINVAL       = -1;
        public const int Z_EPARSE       = -2;
        public const int Z_EIO          = -3;
        public const int Z_ENETWORK     = -4;
        public const int Z_ENULL        = -5;
        public const int Z_EUNAVAILABLE = -6;
        public const int Z_EDESERIALIZE = -7;
        public const int Z_ESESSION_CLOSED = -8;
        public const int Z_EGENERIC     = -128; // INT8_MIN

        public static string GetErrorString(int code) => code switch
        {
            Z_OK             => "OK",
            Z_EINVAL         => "Invalid argument",
            Z_EPARSE         => "Parse error",
            Z_EIO            => "I/O error",
            Z_ENETWORK       => "Network error",
            Z_ENULL          => "Null pointer",
            Z_EUNAVAILABLE   => "Unavailable",
            Z_EDESERIALIZE   => "Deserialization error",
            Z_ESESSION_CLOSED => "Session closed",
            Z_EGENERIC       => "Generic error",
            _                => $"Unknown error ({code})"
        };
    }

    /// <summary>
    /// P/Invoke bindings for zenoh-c native library aligned with v1.x ABI.
    ///
    /// Key API differences from v0.x → v1.x:
    ///  - z_open: now takes 3 args (session*, moved_config*, open_options*)
    ///  - Config must be created explicitly via z_config_default before z_open
    ///  - z_close replaced by z_session_drop (what z_drop(z_move(s)) calls in C)
    ///  - Subscriber/Queryable use closure structs instead of raw fn-pointers
    ///  - z_publisher_put uses z_owned_bytes_t instead of (buf, len)
    ///  - z_declare_publisher/subscriber take a "loaned" session (same memory, ref)
    ///
    /// DLL placement (Windows x64):
    ///   Place zenohc.dll (from zenoh-c-1.7.2-x86_64-pc-windows-msvc-standalone)
    ///   alongside the executable, or in src/ZROS.Core/native/win-x64/native/zenohc.dll
    ///   for NuGet packaging.
    /// </summary>
    public static class ZenohNative
    {
        private const string LibName = "zenohc";

        // ── Config ──────────────────────────────────────────────────────

        /// <summary>
        /// Initialises <paramref name="config"/> with zenoh default settings.
        /// C sig: void z_config_default(z_owned_config_t*)
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_config_default", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_config_default(out z_owned_config_t config);

        /// <summary>
        /// Drops (frees) an owned config that was not consumed by z_open.
        /// C sig: void z_config_drop(z_moved_config_t*)
        /// z_moved_config_t* is layout-compatible with z_owned_config_t*.
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_config_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_config_drop(ref z_owned_config_t config);

        // ── Session ─────────────────────────────────────────────────────

        /// <summary>
        /// Opens a Zenoh session.
        /// C sig: z_result_t z_open(z_owned_session_t*, z_moved_config_t*, const z_open_options_t*)
        ///
        /// Pass <c>IntPtr.Zero</c> for <paramref name="options"/> to use defaults.
        /// On success the config is consumed (ownership transferred); on failure
        /// call z_config_drop to release it.
        /// Returns Z_OK (0) on success, negative error code on failure.
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_open(
            out z_owned_session_t session,
            ref z_owned_config_t config,
            IntPtr options);

        /// <summary>
        /// Drops (closes) an owned session.
        /// C sig: void z_session_drop(z_moved_session_t*)
        /// This is what z_drop(z_move(session)) resolves to in the C headers.
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_session_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_session_drop(ref z_owned_session_t session);

        // ── Key expression ───────────────────────────────────────────────

        /// <summary>
        /// Borrows a string as a key expression (no allocation; <paramref name="expr"/> must
        /// outlive <paramref name="keyexpr"/>).
        /// C sig: z_result_t z_view_keyexpr_from_str(z_view_keyexpr_t*, const char*)
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_view_keyexpr_from_str",
            CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int z_view_keyexpr_from_str(out z_view_keyexpr_t keyexpr, string expr);

        // ── Publisher ───────────────────────────────────────────────────

        /// <summary>
        /// Declares a publisher on the given key expression.
        /// C sig: z_result_t z_declare_publisher(z_loaned_session_t*, z_owned_publisher_t*,
        ///            z_loaned_keyexpr_t*, const z_publisher_options_t*)
        ///
        /// z_loaned_session_t* / z_loaned_keyexpr_t* are layout-compatible with the owned types.
        /// Pass <c>IntPtr.Zero</c> for <paramref name="options"/> to use defaults.
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_declare_publisher",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_publisher(
            ref z_owned_session_t session,
            out z_owned_publisher_t publisher,
            ref z_view_keyexpr_t keyexpr,
            IntPtr options);

        /// <summary>
        /// Drops (undeclares) an owned publisher.
        /// C sig: void z_publisher_drop(z_moved_publisher_t*)
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_publisher_drop",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_publisher_drop(ref z_owned_publisher_t publisher);

        /// <summary>
        /// Publishes a payload on a declared publisher.
        /// C sig: z_result_t z_publisher_put(z_loaned_publisher_t*, z_moved_bytes_t*,
        ///            const z_publisher_put_options_t*)
        ///
        /// Ownership of <paramref name="payload"/> is transferred on success.
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_publisher_put",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_publisher_put(
            ref z_owned_publisher_t publisher,
            ref z_owned_bytes_t payload,
            IntPtr options);

        // ── Bytes ───────────────────────────────────────────────────────

        /// <summary>
        /// Copies raw bytes into an owned bytes payload.
        /// C sig: void z_bytes_copy_from_buf(z_owned_bytes_t*, const uint8_t*, size_t)
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_bytes_copy_from_buf",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_bytes_copy_from_buf(
            out z_owned_bytes_t bytes,
            IntPtr data,
            UIntPtr len);

        /// <summary>
        /// Drops owned bytes.
        /// C sig: void z_bytes_drop(z_moved_bytes_t*)
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_bytes_drop",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_bytes_drop(ref z_owned_bytes_t bytes);

        // ── Subscriber ──────────────────────────────────────────────────

        /// <summary>
        /// Declares a subscriber with a sample callback closure.
        /// C sig: z_result_t z_declare_subscriber(z_loaned_session_t*, z_owned_subscriber_t*,
        ///            z_loaned_keyexpr_t*, z_moved_closure_sample_t*, const z_subscriber_options_t*)
        ///
        /// Ownership of <paramref name="callback"/> is transferred.
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_declare_subscriber",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_subscriber(
            ref z_owned_session_t session,
            out z_owned_subscriber_t subscriber,
            ref z_view_keyexpr_t keyexpr,
            ref z_owned_closure_sample_t callback,
            IntPtr options);

        /// <summary>
        /// Drops (undeclares) an owned subscriber.
        /// C sig: void z_subscriber_drop(z_moved_subscriber_t*)
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_subscriber_drop",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_subscriber_drop(ref z_owned_subscriber_t subscriber);

        // ── Queryable ───────────────────────────────────────────────────

        /// <summary>
        /// Declares a queryable with a query callback closure.
        /// C sig: z_result_t z_declare_queryable(z_loaned_session_t*, z_owned_queryable_t*,
        ///            z_loaned_keyexpr_t*, z_moved_closure_query_t*, const z_queryable_options_t*)
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_declare_queryable",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_queryable(
            ref z_owned_session_t session,
            out z_owned_queryable_t queryable,
            ref z_view_keyexpr_t keyexpr,
            ref z_owned_closure_query_t callback,
            IntPtr options);

        /// <summary>
        /// Drops (undeclares) an owned queryable.
        /// C sig: void z_queryable_drop(z_moved_queryable_t*)
        /// </summary>
        [DllImport(LibName, EntryPoint = "z_queryable_drop",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_queryable_drop(ref z_owned_queryable_t queryable);

        // ── Utility ─────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to verify that the native zenoh-c library is loadable and
        /// its entry points exist.  Returns <c>true</c> on success, <c>false</c>
        /// when the DLL is absent or an entry point is missing.
        ///
        /// A successful call opens a session with default config and immediately
        /// closes it; no network traffic is generated for a local-only config.
        /// </summary>
        public static bool TryLoad()
        {
            try
            {
                z_config_default(out var config);
                int result = z_open(out var session, ref config, IntPtr.Zero);
                if (result == ZenohErrorCodes.Z_OK)
                {
                    z_session_drop(ref session);
                }
                else
                {
                    z_config_drop(ref config);
                }
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
                // Any other exception (e.g. bad DLL format) – treat as unavailable
                return false;
            }
        }
    }
}
