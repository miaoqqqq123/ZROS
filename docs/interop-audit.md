# zenoh-c Interop Audit

_Last updated: 2026-03-12_

## 1. Repository layout (post PR #4 + #5)

After merging PR #4 and PR #5 the solution contains exactly six active projects:

```
ZROS.sln
├── src/
│   ├── ZROS.Core/              # P/Invoke bindings + ROS node/pub/sub/service/action APIs
│   ├── ZROS.Messages/          # ROS message types (std_msgs, geometry_msgs, …)
│   ├── ZROS.ServiceManager/    # Service lifecycle manager
│   └── ZROS.ServiceManager.UI/ # WPF dashboard (net8.0-windows)
├── tests/
│   ├── ZROS.Tests/             # Core unit tests (xUnit)
│   └── ZROS.ServiceManager.Tests/
└── samples/
    └── ZROS.ZenohInteropDemo/  # NEW – interop smoke-test demo (added in this PR)
```

### Legacy projects removed (PR #4 + #5)

| Removed project | Reason |
|-----------------|--------|
| `src/Ros2.Actions/`     | Superseded by `ZROS.Core` |
| `src/Ros2.Core/`        | Superseded by `ZROS.Core` |
| `src/Ros2.Messaging/`   | Superseded by `ZROS.Messages` |
| `src/Ros2.Pub/`         | Superseded by `ZROS.Core` |
| `src/Ros2.Services/`    | Superseded by `ZROS.Core` |
| `src/Zenoh.Native/`     | Superseded by `ZROS.Core/Native/` |

`dotnet sln ZROS.sln list` should produce exactly the six active projects above.

---

## 2. zenoh-c v1.x API alignment

### 2.1 What changed between v0.x and v1.x

| Concern | v0.x (old) | v1.x (current) |
|---------|-----------|----------------|
| `z_open` arity | 2 args `(session*, config*)` | **3 args** `(session*, moved_config*, open_options*)` |
| Config creation | Implicit / null was accepted | Explicit: `z_config_default(out config)` |
| Session close | `z_close(ref session)` (1 arg) | `z_session_drop(ref session)` (what `z_drop(z_move(s))` calls) |
| Publisher close | `z_undeclare_publisher` | `z_publisher_drop` |
| Subscriber close | `z_undeclare_subscriber` | `z_subscriber_drop` |
| Subscriber callback | Raw `fn(sample*, arg*)` ptr | Closure struct `{context*, call*, drop*}` |
| Publisher put payload | `(buf*, len)` pair | `z_owned_bytes_t*` |
| Key expression (view) | `z_keyexpr_t {id, suffix}` | `z_view_keyexpr_t` (opaque, created via `z_view_keyexpr_from_str`) |
| Return type of most fns | `int` | `z_result_t` (`int8_t`) |
| Error codes | Non-standard | Standard (see §2.3) |

### 2.2 Root cause of `AccessViolationException`

The old `ZenohNative.cs` declared `z_open` with **2 parameters**:

```csharp
// OLD – wrong: missing options parameter
public static extern int z_open(out z_owned_session_t session, IntPtr config);
```

The zenoh-c v1.x ABI has **3 parameters**:

```c
z_result_t z_open(z_owned_session_t *session,
                  z_moved_config_t  *config,
                  const z_open_options_t *options);
```

On the Microsoft x64 calling convention the third argument is placed in the `R8`
register; when the C# side does not populate it, the function reads garbage from
`R8` and immediately tries to dereference it as a pointer → **access violation**.

Additionally, passing `IntPtr.Zero` for `config` caused a null-pointer dereference
inside `z_open` (config is mandatory in v1.x).

### 2.3 Error code mapping

| Constant | v0.x value | v1.x value (zenoh_concrete.h) |
|----------|-----------|-------------------------------|
| `Z_OK`            |  0  |  0  |
| `Z_EINVAL`        | -1  | -1  |
| `Z_EPARSE`        | -2  | -2  |
| `Z_ENOMEM` (old)  | -3  | _(removed)_ |
| `Z_EIO`           | —   | **-3** |
| `Z_ECONNREFUSED` (old) | -4 | _(removed)_ |
| `Z_ENETWORK`      | —   | **-4** |
| `Z_ENULL`         | —   | **-5** |
| `Z_EUNAVAILABLE`  | —   | **-6** |
| `Z_EDESERIALIZE`  | —   | **-7** |
| `Z_ESESSION_CLOSED` | -5 | **-8** |
| `Z_EGENERIC`      | —   | **-128** (`INT8_MIN`) |

### 2.4 Struct sizes

Sizes from `zenoh-c-1.7.2-x86_64-pc-windows-msvc-standalone/include/zenoh_opaque.h`:

| Struct | C# `[StructLayout(Size = N)]` | Confirmed? |
|--------|-------------------------------|-----------|
| `z_owned_session_t`   | 8   | ✅ from headers |
| `z_owned_config_t`    | 8   | Estimated (single pointer) |
| `z_owned_publisher_t` | 112 | ✅ from headers |
| `z_owned_subscriber_t`| 8   | Estimated |
| `z_owned_queryable_t` | 8   | Estimated |
| `z_view_keyexpr_t`    | 32  | Estimated (generous upper bound) |
| `z_owned_bytes_t`     | 48  | Estimated (generous upper bound) |
| `z_owned_closure_sample_t` | 24 | ✅ concrete (3 × IntPtr) |

**Action required**: compare all "Estimated" entries against `zenoh_opaque.h` and
update the `ZenohAbiSizes` constants in `ZenohNative.cs` accordingly.
Mismatches will cause memory corruption and crashes in native mode.

---

## 3. Changes made in this PR

### `src/ZROS.Core/Native/ZenohNative.cs` (full rewrite)

- **Struct definitions** updated to `[StructLayout(LayoutKind.Sequential, Size = N)]`
  with sizes documented in `ZenohAbiSizes`.
- **`ZenohErrorCodes`** updated to match zenoh-c 1.x `zenoh_concrete.h`.
- **`z_config_default` / `z_config_drop`** added.
- **`z_open`** corrected to 3-parameter signature.
- **`z_session_drop`** added (replaces `z_close`).
- **`z_view_keyexpr_from_str`** added.
- **`z_declare_publisher` / `z_publisher_drop` / `z_publisher_put`** updated for v1.x.
- **`z_declare_subscriber` / `z_subscriber_drop`** updated (closure-based callback).
- **`z_declare_queryable` / `z_queryable_drop`** updated.
- **`z_bytes_copy_from_buf` / `z_bytes_drop`** added.
- **Closure types** `z_owned_closure_sample_t` and `z_owned_closure_query_t` added.
- **`TryLoad()`** fixed to create config before calling `z_open`.

### `src/ZROS.Core/RosContext.cs`

- Constructor calls `z_config_default` then `z_open(session, config, options)`.
- Config is dropped via `z_config_drop` when `z_open` fails.
- `Dispose` calls `z_session_drop` instead of `z_close`.

### `samples/ZROS.ZenohInteropDemo/` (new)

New console project that:
1. Verifies struct sizes at runtime.
2. Probes native library via `ZenohNative.TryLoad()`.
3. Opens a `RosContext` and exercises node/publisher/subscriber.
4. Prints the full error-code table.

Added to `ZROS.sln`.

### `tests/ZROS.Tests/ZenohInteropTests.cs` (new)

- Struct-size assertions for all zenoh-c types.
- `Unsafe.SizeOf` vs `Marshal.SizeOf` cross-check.
- Error-code alignment test.
- Simulation-mode smoke test (no native DLL required).

---

## 4. How to verify everything is correct

```bash
# Build the entire solution
dotnet build ZROS.sln -c Release

# Run all tests (no native DLL needed)
dotnet test ZROS.sln

# Run interop-specific tests only
dotnet test tests/ZROS.Tests --filter "Category=ZenohInterop"

# Run the demo (simulation mode if DLL absent)
dotnet run --project samples/ZROS.ZenohInteropDemo
```

To test with a real zenoh-c DLL:
1. Copy `zenohc.dll` to `samples/ZROS.ZenohInteropDemo/bin/Debug/net8.0/`.
2. Run `dotnet run --project samples/ZROS.ZenohInteropDemo`.
3. Verify `IsSimulated=False` in the output.
