# ZROS.ZenohInteropDemo

Smoke-test and verification harness for the zenoh-c v1.x P/Invoke bindings in
`src/ZROS.Core/Native/ZenohNative.cs`.

## What it does

| Step | Description |
|------|-------------|
| 1 | **ABI struct size verification** – prints each C# struct's `Marshal.SizeOf<T>()` and the expected value from `ZenohAbiSizes`, flagging any mismatch. |
| 2 | **Native library probe** – calls `ZenohNative.TryLoad()` which attempts `z_config_default` → `z_open` → `z_session_drop`. |
| 3–5 | **RosContext open/close + node/pub/sub** – exercises the high-level ZROS API in either native or simulation mode. |
| 6 | **Error-code table** – prints all zenoh-c 1.x error codes from `ZenohErrorCodes`. |

## Running without a native library (simulation mode)

The project compiles and runs **without** `zenohc.dll` present.  Simulation mode
is activated automatically when the DLL is absent.

```bash
cd <repo-root>
dotnet run --project samples/ZROS.ZenohInteropDemo
```

Expected output ends with:
```
=== Demo completed successfully ===
```

## Running with the real zenoh-c native library (Windows x64)

### Step 1 – obtain the library

Download
`zenoh-c-<version>-x86_64-pc-windows-msvc-standalone.zip`
from the [zenoh-c releases page](https://github.com/eclipse-zenoh/zenoh-c/releases)
and extract it.  The standalone zip contains `zenohc.dll` (and sometimes `zenohc.pdb`)
inside a `lib/` or root directory.

### Step 2 – place the library

**Option A – alongside the executable (quick test)**

Copy `zenohc.dll` into the build output directory:

```
samples/ZROS.ZenohInteropDemo/bin/Debug/net8.0/zenohc.dll
```

Then run:

```powershell
dotnet run --project samples/ZROS.ZenohInteropDemo
```

**Option B – for NuGet packaging (recommended for CI/CD)**

Place `zenohc.dll` under the per-platform native asset directory in `ZROS.Core`:

```
src/ZROS.Core/native/win-x64/native/zenohc.dll
```

When you `dotnet pack src/ZROS.Core`, this file is included at
`runtimes/win-x64/native/zenohc.dll` inside the `.nupkg`.  .NET's native-asset
resolver then copies it next to the executable at build/publish time.

### Step 3 – verify no AccessViolationException

If `IsSimulated=False` appears in the output, the session opened successfully via
the native library.  If you still see `IsSimulated=True`, check:

| Symptom | Likely cause |
|---------|--------------|
| `DllNotFoundException` | `zenohc.dll` not found on DLL search path |
| `BadImageFormatException` | Wrong architecture (e.g. x86 DLL on x64 process) |
| `AccessViolationException` | ABI mismatch – struct sizes or function signatures wrong; verify against `zenoh_opaque.h` |
| Error code ≠ 0 from `z_open` | Zenoh router not reachable (harmless for local-only peer config) |

## Verifying struct sizes against real headers

If you have the
`zenoh-c-<version>-x86_64-pc-windows-msvc-standalone/include`
headers, compare the sizes in `zenoh_opaque.h` with `ZenohAbiSizes` in
`src/ZROS.Core/Native/ZenohNative.cs`:

```c
// From zenoh_opaque.h (example layout):
typedef struct z_owned_session_t {
    uint8_t _0[8];      // → ZenohAbiSizes.SessionBytes   = 8
} z_owned_session_t;

typedef struct z_owned_publisher_t {
    uint8_t _0[112];    // → ZenohAbiSizes.PublisherBytes  = 112
} z_owned_publisher_t;
```

Update any mismatched constant in `ZenohAbiSizes` and re-run the tests:

```bash
dotnet test tests/ZROS.Tests --filter Category=ZenohInterop
```

## Running the dedicated interop tests

```bash
dotnet test tests/ZROS.Tests --filter "Category=ZenohInterop" -v normal
```

These tests verify struct sizes and error-code alignment without requiring
the native library.
