# ZROS.Core Native Libraries

This directory contains the zenoh-c native libraries bundled with the `ZROS.Core` NuGet package.
Place the compiled zenoh-c binaries in the appropriate Runtime Identifier (RID) subdirectory.

## Directory Structure

```
native/
├── win-x64/native/zenohc.dll
├── win-arm64/native/zenohc.dll
├── linux-x64/native/libzenohc.so
├── linux-arm64/native/libzenohc.so
├── osx-x64/native/libzenohc.dylib
└── osx-arm64/native/libzenohc.dylib
```

## How to Obtain zenoh-c Binaries

Download pre-built zenoh-c releases from:
https://github.com/eclipse-zenoh/zenoh-c/releases

For each target platform, download the appropriate archive and extract the shared library into the corresponding directory above.

### Example (Linux x64)

```bash
# Download and extract the linux-x64 release
wget https://github.com/eclipse-zenoh/zenoh-c/releases/download/<version>/zenoh-c-<version>-x86_64-unknown-linux-gnu-shared.zip
unzip zenoh-c-<version>-x86_64-unknown-linux-gnu-shared.zip
cp libzenohc.so native/linux-x64/native/libzenohc.so
```

## Behavior Without Native Libraries

If the native library is not present for the current platform, `ZROS.Core` will automatically fall
back to **simulation mode** (`RosContext.IsSimulated == true`). No exception is thrown at startup.
This allows development and unit testing on any platform without requiring the zenoh runtime.

If you attempt to perform operations that require a real Zenoh connection in simulation mode,
a `NotSupportedException` or `NotImplementedException` will be thrown with a descriptive message.

## NuGet Packaging

The `ZROS.Core.csproj` is configured to automatically include any binaries placed here when
running `dotnet pack`. The binaries are mapped to the NuGet `runtimes/<rid>/native/` convention,
so they are automatically resolved by the .NET runtime on the target platform.
