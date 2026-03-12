// ZROS.ZenohInteropDemo – smoke test for zenoh-c v1.x P/Invoke bindings.
//
// Run without a zenoh-c DLL to verify simulation-mode fallback.
// Copy zenohc.dll (Windows x64) next to the executable to exercise real bindings.
//
// Usage:
//   dotnet run --project samples/ZROS.ZenohInteropDemo
//
// See README.md in this directory for setup instructions.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ZROS.Core;
using ZROS.Core.Native;
using ZROS.Messages.std_msgs;

Console.WriteLine("=== ZROS ZenohInteropDemo ===");
Console.WriteLine();

// ── Step 1: struct-size verification ───────────────────────────────────────
Console.WriteLine("--- Step 1: ABI struct size verification ---");
VerifySize<z_owned_session_t>("z_owned_session_t",       ZenohAbiSizes.SessionBytes,      confirmed: true);
VerifySize<z_owned_config_t>("z_owned_config_t",         ZenohAbiSizes.ConfigBytes,       confirmed: false);
VerifySize<z_owned_publisher_t>("z_owned_publisher_t",   ZenohAbiSizes.PublisherBytes,    confirmed: true);
VerifySize<z_owned_subscriber_t>("z_owned_subscriber_t", ZenohAbiSizes.SubscriberBytes,   confirmed: false);
VerifySize<z_owned_queryable_t>("z_owned_queryable_t",   ZenohAbiSizes.QueryableBytes,    confirmed: false);
VerifySize<z_view_keyexpr_t>("z_view_keyexpr_t",         ZenohAbiSizes.ViewKeyexprBytes,  confirmed: false);
VerifySize<z_owned_bytes_t>("z_owned_bytes_t",           ZenohAbiSizes.OwnedBytesBytes,   confirmed: false);
VerifySize<z_owned_closure_sample_t>("z_owned_closure_sample_t", ZenohAbiSizes.ClosureSampleBytes, confirmed: true);
Console.WriteLine();

// ── Step 2: native library probe ───────────────────────────────────────────
Console.WriteLine("--- Step 2: native library probe (ZenohNative.TryLoad) ---");
bool nativeAvailable = ZenohNative.TryLoad();
Console.WriteLine(nativeAvailable
    ? "[OK] zenoh-c native library loaded and entry-points verified."
    : "[OK] zenoh-c native library NOT found – will use simulation mode.");
Console.WriteLine();

// ── Step 3: RosContext open/close ──────────────────────────────────────────
Console.WriteLine("--- Step 3: RosContext open / close ---");
using (var ctx = RosContext.Create())
{
    Console.WriteLine($"  IsOpen      = {ctx.IsOpen}");
    Console.WriteLine($"  IsSimulated = {ctx.IsSimulated}");

    if (ctx.IsSimulated)
        Console.WriteLine("[OK] Session in SIMULATION mode (zenoh-c DLL absent or not valid).");
    else
        Console.WriteLine("[OK] Session in REAL (native) mode.");

    // ── Step 4: Node creation ───────────────────────────────────────────────
    Console.WriteLine();
    Console.WriteLine("--- Step 4: node creation ---");
    using var node = ctx.CreateNode("interop_demo");
    Console.WriteLine($"  Node '{node.Name}' in namespace '{node.Namespace}' created.");

    // ── Step 5: Publisher + subscriber smoke test ───────────────────────────
    Console.WriteLine();
    Console.WriteLine("--- Step 5: publish / subscribe via high-level API ---");

    StringMsg? received = null;
    var latch = new ManualResetEventSlim(false);
    const string Topic = "/zros/interop_demo";

    using var subscriber = new RosSubscriber<StringMsg>(node, Topic, msg =>
    {
        received = msg;
        latch.Set();
    });

    using var publisher = new RosPublisher<StringMsg>(node, Topic);

    var message = new StringMsg("Hello from ZenohInteropDemo!");
    publisher.Publish(message);

    // In simulation mode ZROS has no in-process message bus: the publisher
    // serialises the message but there is no router to deliver it to subscribers.
    // (Use subscriber.SimulateReceive() to inject test data manually.)
    // In real (native) mode give the subscriber up to 2 seconds.
    bool gotIt = ctx.IsSimulated
        ? false  // not expected in sim mode – check only PublishedCount
        : latch.Wait(TimeSpan.FromSeconds(2));

    if (ctx.IsSimulated)
    {
        if (publisher.PublishedCount == 1)
            Console.WriteLine($"  [OK] Published message successfully (simulation mode – no in-process routing).");
        else
            Console.WriteLine($"  [WARN] Unexpected PublishedCount={publisher.PublishedCount}.");
    }
    else if (gotIt && received?.Data == message.Data)
        Console.WriteLine($"  [OK] Published and received: '{received!.Data}'");
    else if (gotIt)
        Console.WriteLine($"  [WARN] Received unexpected content: '{received?.Data}'");
    else
        Console.WriteLine("  [WARN] Subscriber did not receive message within 2 s (expected in router mode).");

    Console.WriteLine($"  PublishedCount = {publisher.PublishedCount}");
    Console.WriteLine($"  ReceivedCount  = {subscriber.ReceivedCount}");
}
Console.WriteLine("  RosContext disposed.");
Console.WriteLine();

// ── Step 6: error-code table ───────────────────────────────────────────────
Console.WriteLine("--- Step 6: error-code table (zenoh-c 1.x) ---");
int[] codes = { 0, -1, -2, -3, -4, -5, -6, -7, -8, -128 };
foreach (var c in codes)
    Console.WriteLine($"  {c,5} → {ZenohErrorCodes.GetErrorString(c)}");
Console.WriteLine();

Console.WriteLine("=== Demo completed successfully ===");

// ---------------------------------------------------------------------------

static void VerifySize<T>(string name, int expected, bool confirmed) where T : struct
{
    int actual = Marshal.SizeOf<T>();
    string status = actual == expected ? "[OK]  " : "[WARN]";
    string note   = confirmed ? "(confirmed from headers)" : "(estimated – verify against zenoh_opaque.h)";
    Console.WriteLine($"  {status} {name,-28} = {actual,4} bytes  expected={expected,4}  {note}");
}

