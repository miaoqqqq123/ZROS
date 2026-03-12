# ZROS — Zenoh-based ROS2-like Framework for .NET

ZROS is a .NET 8 framework that provides ROS2-style communication primitives (Pub/Sub, Services, Actions) built on top of [Zenoh](https://zenoh.io/).

## Packages

| Package | Description |
|---------|-------------|
| **ZROS.Core** | Core runtime: Zenoh P/Invoke, Pub/Sub, Services, Actions, serialization, and logging |
| **ZROS.Messages** | Standard message types (std_msgs, geometry_msgs, etc.) |
| **ZROS.NativeBindingDemo** | 验证 zenoh-c 1.7.2 P/Invoke 原生绑定层的控制台测试程序 |

## Quick Start

```csharp
using ZROS.Core;
using ZROS.Messages.std_msgs;

// Create a context and node
using var context = RosContext.Create();
using var node = context.CreateNode("my_node");

// Publisher
using var publisher = new RosPublisher<StringMsg>(node, "/chatter");
publisher.Publish(new StringMsg("Hello, ZROS!"));

// Subscriber
using var subscriber = new RosSubscriber<StringMsg>(node, "/chatter", msg =>
{
    Console.WriteLine($"Received: {msg.Data}");
});
```

## Custom Messages

ZROS.Core defines the `IMessage` interface. You can use your own message types without depending on ZROS.Messages:

```csharp
using ZROS.Core;

public class MyCustomMsg : IMessage
{
    public string MessageType => "my_pkg/MyCustomMsg";
    public double Value { get; set; }
}
```

## Simulation Mode

When the native zenoh-c library is not available, ZROS automatically falls back to **simulation mode** (`RosContext.IsSimulated == true`). This allows development and testing without a Zenoh daemon.

## Logging

ZROS.Core uses NLog by default. Initialize logging before use:

```csharp
using ZROS.Core.Logging;

ZrosLoggerFactory.Initialize();          // Use built-in defaults
// or
ZrosLoggerFactory.Initialize("nlog.config");  // Custom config file
```

## Native Library Setup

ZROS.Core includes placeholders for the zenoh-c native binaries. See [`src/ZROS.Core/native/README.md`](src/ZROS.Core/native/README.md) for instructions on obtaining and placing the native libraries.

## Building

```bash
dotnet restore
dotnet build ZROS.sln -c Debug
dotnet test
dotnet sln ZROS.sln list
```

运行 P/Invoke 原生绑定验证 Demo（无需 zenoh-c DLL 即可运行）：

```bash
dotnet run --project samples/ZROS.NativeBindingDemo
```
