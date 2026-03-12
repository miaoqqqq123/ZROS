using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ZROS.Core.Native;
using Xunit;

namespace ZROS.Tests
{
    /// <summary>
    /// Verifies that the C# struct sizes declared in ZenohNative.cs match the ABI sizes
    /// documented in zenoh-c 1.7.2 headers (zenoh_opaque.h, zenoh_concrete.h).
    ///
    /// Sizes annotated as "confirmed" come directly from the
    /// zenoh-c-1.7.2-x86_64-pc-windows-msvc-standalone headers.
    /// Sizes annotated as "estimated" should be validated against the actual
    /// zenoh_opaque.h for your specific zenoh-c build before relying on them.
    ///
    /// Run these tests with: dotnet test --filter Category=ZenohInterop
    /// </summary>
    [Trait("Category", "ZenohInterop")]
    public class ZenohInteropTests
    {
        // ── Struct size assertions ──────────────────────────────────────

        [Fact(DisplayName = "z_owned_session_t must be 8 bytes (confirmed from zenoh-c 1.7.2 headers)")]
        public void Session_StructSize_Matches_ABI()
        {
            Assert.Equal(ZenohAbiSizes.SessionBytes, Marshal.SizeOf<z_owned_session_t>());
        }

        [Fact(DisplayName = "z_owned_config_t must be 8 bytes (estimated: single opaque pointer)")]
        public void Config_StructSize_Matches_ABI()
        {
            Assert.Equal(ZenohAbiSizes.ConfigBytes, Marshal.SizeOf<z_owned_config_t>());
        }

        [Fact(DisplayName = "z_owned_publisher_t must be 112 bytes (confirmed from zenoh-c 1.7.2 headers)")]
        public void Publisher_StructSize_Matches_ABI()
        {
            Assert.Equal(ZenohAbiSizes.PublisherBytes, Marshal.SizeOf<z_owned_publisher_t>());
        }

        [Fact(DisplayName = "z_owned_subscriber_t must be 8 bytes (estimated: single opaque handle)")]
        public void Subscriber_StructSize_Matches_ABI()
        {
            Assert.Equal(ZenohAbiSizes.SubscriberBytes, Marshal.SizeOf<z_owned_subscriber_t>());
        }

        [Fact(DisplayName = "z_owned_queryable_t must be 8 bytes (estimated: single opaque handle)")]
        public void Queryable_StructSize_Matches_ABI()
        {
            Assert.Equal(ZenohAbiSizes.QueryableBytes, Marshal.SizeOf<z_owned_queryable_t>());
        }

        [Fact(DisplayName = "z_view_keyexpr_t must be at least 8 bytes (estimated upper bound 32)")]
        public void ViewKeyexpr_StructSize_AtLeast_MinimumSize()
        {
            // z_view_keyexpr_t stores a borrowed reference to a key-expression string.
            // Minimum on x64: one pointer = 8 bytes.  We allocate 32 bytes as a safe
            // upper bound.  If the actual size exceeds this, reduce ZenohAbiSizes.ViewKeyexprBytes.
            int actualSize = Marshal.SizeOf<z_view_keyexpr_t>();
            Assert.Equal(ZenohAbiSizes.ViewKeyexprBytes, actualSize);
        }

        [Fact(DisplayName = "z_owned_bytes_t must be at least 8 bytes (estimated upper bound 48)")]
        public void OwnedBytes_StructSize_AtLeast_MinimumSize()
        {
            int actualSize = Marshal.SizeOf<z_owned_bytes_t>();
            Assert.Equal(ZenohAbiSizes.OwnedBytesBytes, actualSize);
        }

        [Fact(DisplayName = "z_owned_closure_sample_t must be 24 bytes (3 × IntPtr on x64)")]
        public void ClosureSample_StructSize_Matches_ABI()
        {
            // Concrete type from zenoh_commons.h: { void* context; fnptr call; fnptr drop }
            Assert.Equal(ZenohAbiSizes.ClosureSampleBytes, Marshal.SizeOf<z_owned_closure_sample_t>());
        }

        [Fact(DisplayName = "z_owned_closure_query_t must be 24 bytes (3 × IntPtr on x64)")]
        public void ClosureQuery_StructSize_Matches_ABI()
        {
            Assert.Equal(24, Marshal.SizeOf<z_owned_closure_query_t>());
        }

        // ── Unsafe.SizeOf cross-check ───────────────────────────────────

        [Fact(DisplayName = "Unsafe.SizeOf matches Marshal.SizeOf for all types")]
        public void UnsafeSizeOf_Matches_MarshalSizeOf()
        {
            // Unsafe.SizeOf<T>() is what is used at the JIT level; Marshal.SizeOf<T>() is
            // used by the P/Invoke marshaller.  They must agree for these blittable types.
            Assert.Equal(Marshal.SizeOf<z_owned_session_t>(),   Unsafe.SizeOf<z_owned_session_t>());
            Assert.Equal(Marshal.SizeOf<z_owned_config_t>(),    Unsafe.SizeOf<z_owned_config_t>());
            Assert.Equal(Marshal.SizeOf<z_owned_publisher_t>(), Unsafe.SizeOf<z_owned_publisher_t>());
            Assert.Equal(Marshal.SizeOf<z_owned_subscriber_t>(), Unsafe.SizeOf<z_owned_subscriber_t>());
            Assert.Equal(Marshal.SizeOf<z_owned_queryable_t>(), Unsafe.SizeOf<z_owned_queryable_t>());
            Assert.Equal(Marshal.SizeOf<z_view_keyexpr_t>(),    Unsafe.SizeOf<z_view_keyexpr_t>());
            Assert.Equal(Marshal.SizeOf<z_owned_bytes_t>(),     Unsafe.SizeOf<z_owned_bytes_t>());
            Assert.Equal(Marshal.SizeOf<z_owned_closure_sample_t>(), Unsafe.SizeOf<z_owned_closure_sample_t>());
        }

        // ── Error-code alignment ────────────────────────────────────────

        [Fact(DisplayName = "ZenohErrorCodes match zenoh-c 1.x zenoh_concrete.h")]
        public void ErrorCodes_Match_ZenohV1x_Headers()
        {
            Assert.Equal(0,    ZenohErrorCodes.Z_OK);
            Assert.Equal(-1,   ZenohErrorCodes.Z_EINVAL);
            Assert.Equal(-2,   ZenohErrorCodes.Z_EPARSE);
            Assert.Equal(-3,   ZenohErrorCodes.Z_EIO);
            Assert.Equal(-4,   ZenohErrorCodes.Z_ENETWORK);
            Assert.Equal(-5,   ZenohErrorCodes.Z_ENULL);
            Assert.Equal(-6,   ZenohErrorCodes.Z_EUNAVAILABLE);
            Assert.Equal(-7,   ZenohErrorCodes.Z_EDESERIALIZE);
            Assert.Equal(-8,   ZenohErrorCodes.Z_ESESSION_CLOSED);
            Assert.Equal(-128, ZenohErrorCodes.Z_EGENERIC);
        }

        // ── Simulation-mode smoke test ──────────────────────────────────

        [Fact(DisplayName = "RosContext falls back to simulation when zenoh-c DLL is absent")]
        public void RosContext_SimulationMode_WhenNativeDllAbsent()
        {
            // The test environment has no zenohc.dll present, so the context
            // must silently enter simulation mode rather than crashing.
            using var ctx = new ZROS.Core.RosContext();
            Assert.True(ctx.IsSimulated,
                "Expected simulation mode when native zenoh-c library is not present.");
            Assert.True(ctx.IsOpen,
                "Context should still report IsOpen=true in simulation mode.");
        }
    }
}
