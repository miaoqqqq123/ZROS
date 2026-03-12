// ===========================================================================
// ZROS.NativeBindingDemo — zenoh-c 1.7.2 P/Invoke 原生绑定验证程序
//
// 本程序分 7 个步骤，逐一测试并验证 ZenohNative.cs 中所有 P/Invoke 绑定的
// 正确性。在无 zenohc.dll 的环境下，步骤 3–5、7 会自动跳过，程序仍能正常
// 运行到结束，不抛出未处理的异常。
//
// 使用方法：
//   dotnet run --project samples/ZROS.NativeBindingDemo
//
// 若需要测试真实 zenoh-c 功能，请将 zenohc.dll（Windows x64）放到输出目录旁。
// ===========================================================================

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ZROS.Core.Native;

// ────────────────────────────────────────────────────────────────────────────
// 程序入口
// ────────────────────────────────────────────────────────────────────────────
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("=== ZROS P/Invoke 原生绑定验证程序 (zenoh-c 1.7.2) ===");
Console.WriteLine();

// 步骤 2 的结果（是否有真实原生库）将在后续步骤中判断
bool isSimulated = true; // 默认模拟模式，步骤 2 后更新

// ────────────────────────────────────────────────────────────────────────────
// 步骤 1：ABI struct 尺寸验证
// 使用 Marshal.SizeOf<T>() 打印每个 struct 的实际字节大小，
// 并与 ZenohAbiSizes 中的期望值对比，标记 ✅（匹配）或 ❌（不匹配）。
// ────────────────────────────────────────────────────────────────────────────
Console.WriteLine("【步骤 1】ABI struct 尺寸验证");
Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine($"  {"struct 名称",-35} {"实际大小",8} {"期望大小",8}  {"状态",4}  {"备注",-20}");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

VerifyStructSize<z_owned_session_t>(    "z_owned_session_t",     ZenohAbiSizes.SessionBytes,      "已确认");
VerifyStructSize<z_owned_config_t>(     "z_owned_config_t",      ZenohAbiSizes.ConfigBytes,       "已确认");
VerifyStructSize<z_owned_publisher_t>(  "z_owned_publisher_t",   ZenohAbiSizes.PublisherBytes,    "已确认");
VerifyStructSize<z_owned_subscriber_t>( "z_owned_subscriber_t",  ZenohAbiSizes.SubscriberBytes,   "已确认");
VerifyStructSize<z_owned_queryable_t>(  "z_owned_queryable_t",   ZenohAbiSizes.QueryableBytes,    "已确认");
VerifyStructSize<z_view_keyexpr_t>(     "z_view_keyexpr_t",      ZenohAbiSizes.ViewKeyexprBytes,  "估算上界");
VerifyStructSize<z_owned_bytes_t>(      "z_owned_bytes_t",       ZenohAbiSizes.OwnedBytesBytes,   "估算上界");
VerifyStructSize<z_owned_slice_t>(      "z_owned_slice_t",       ZenohAbiSizes.SliceBytes,        "已确认");
VerifyStructSize<z_owned_closure_sample_t>("z_owned_closure_sample_t", ZenohAbiSizes.ClosureSampleBytes, "已确认");
VerifyStructSize<z_owned_closure_query_t>( "z_owned_closure_query_t",  ZenohAbiSizes.ClosureQueryBytes,  "已确认");

Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine();

// ────────────────────────────────────────────────────────────────────────────
// 步骤 2：原生库探针（TryLoad）
// 调用 ZenohNative.TryLoad() 检测 DLL 是否可加载。
// IsSimulated = true  → 无原生库，后续步骤使用模拟路径
// IsSimulated = false → 真实连接，执行完整测试
// ────────────────────────────────────────────────────────────────────────────
Console.WriteLine("【步骤 2】原生库探针（ZenohNative.TryLoad）");
bool nativeAvailable = ZenohNative.TryLoad();
isSimulated = !nativeAvailable;
Console.WriteLine($"  TryLoad() 结果 = {nativeAvailable}");
Console.WriteLine(nativeAvailable
    ? "  ✅ 原生 zenoh-c 库加载成功，进入真实模式"
    : "  ⚠️  原生 zenoh-c 库未找到，进入模拟模式（IsSimulated = true）");
Console.WriteLine();

// 后续步骤的跳过提示（仅在模拟模式下打印一次）
if (isSimulated)
{
    Console.WriteLine("  [跳过] 原生库未找到，以下步骤 3–5、7 仅在真实 zenoh-c 存在时执行");
    Console.WriteLine();
}

// ────────────────────────────────────────────────────────────────────────────
// 步骤 3：Config + Session 生命周期测试（仅在真实模式）
// 流程：z_config_default → z_open → z_session_loan → z_session_drop
// ────────────────────────────────────────────────────────────────────────────
Console.WriteLine("【步骤 3】Config + Session 生命周期测试");
if (isSimulated)
{
    Console.WriteLine("  [跳过] 模拟模式");
}
else
{
    try
    {
        // 3-1：创建默认配置
        ZenohNative.z_config_default(out var config);
        Console.WriteLine("  ✅ 创建默认配置成功");

        // 3-2：打开会话
        int openResult = ZenohNative.z_open(out var session, ref config, IntPtr.Zero);
        Console.WriteLine($"  z_open 返回码 = {openResult} ({ZenohErrorCodes.GetErrorString(openResult)})");

        if (openResult == ZenohErrorCodes.Z_OK)
        {
            Console.WriteLine("  ✅ 会话打开成功");

            // 3-3：借出只读 session 指针
            IntPtr loanedSession = ZenohNative.z_session_loan(ref session);
            Console.WriteLine($"  z_session_loan 指针地址 = 0x{loanedSession:X16}");
            Console.WriteLine(loanedSession != IntPtr.Zero
                ? "  ✅ loaned session 指针非空（验证通过）"
                : "  ❌ loaned session 指针为 IntPtr.Zero（异常）");

            // 3-4：关闭会话
            ZenohNative.z_session_drop(ref session);
            Console.WriteLine("  ✅ 会话已关闭（z_session_drop 完成）");
        }
        else
        {
            // z_open 失败：需手动释放 config（所有权未被转移）
            ZenohNative.z_config_drop(ref config);
            Console.WriteLine("  ⚠️  会话打开失败，已释放 config");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ 异常：{ex.Message}");
    }
}
Console.WriteLine();

// ────────────────────────────────────────────────────────────────────────────
// 步骤 4：Publisher 测试（仅在真实模式）
// 流程：创建 session → 创建 keyexpr → session_loan → declare_publisher
//        → publisher_loan → bytes_from_buf → publisher_put → publisher_drop → session_drop
// ────────────────────────────────────────────────────────────────────────────
Console.WriteLine("【步骤 4】Publisher 测试");
if (isSimulated)
{
    Console.WriteLine("  [跳过] 模拟模式");
}
else
{
    IntPtr nativeData = IntPtr.Zero;
    try
    {
        // 4-1：打开会话
        ZenohNative.z_config_default(out var config4);
        int r4 = ZenohNative.z_open(out var session4, ref config4, IntPtr.Zero);
        if (r4 != ZenohErrorCodes.Z_OK)
        {
            ZenohNative.z_config_drop(ref config4);
            Console.WriteLine($"  ❌ 无法打开会话（{ZenohErrorCodes.GetErrorString(r4)}），跳过 Publisher 测试");
        }
        else
        {
            Console.WriteLine("  ✅ 会话打开成功");

            // 4-2：创建 key expression（不验证合法性，速度更快）
            const string Topic4 = "demo/native/test";
            ZenohNative.z_view_keyexpr_from_str_unchecked(out var ke4, Topic4);
            Console.WriteLine($"  ✅ 创建 key expression：\"{Topic4}\"");

            // 4-3：借出 session 指针（loaned）
            IntPtr loanedSession4 = ZenohNative.z_session_loan(ref session4);
            // 4-4：借出 keyexpr 指针（loaned）
            IntPtr loanedKe4 = ZenohNative.z_view_keyexpr_loan(ref ke4);

            // 4-5：声明发布者
            int declResult = ZenohNative.z_declare_publisher(loanedSession4, out var publisher4, loanedKe4, IntPtr.Zero);
            Console.WriteLine($"  z_declare_publisher 返回码 = {declResult} ({ZenohErrorCodes.GetErrorString(declResult)})");

            if (declResult == ZenohErrorCodes.Z_OK)
            {
                Console.WriteLine("  ✅ 发布者声明成功");

                // 4-6：借出 publisher 指针（loaned）
                IntPtr loanedPub4 = ZenohNative.z_publisher_loan(ref publisher4);
                Console.WriteLine($"  z_publisher_loan 指针地址 = 0x{loanedPub4:X16}");

                // 4-7：准备消息载荷（UTF-8 字节）
                byte[] msgBytes = Encoding.UTF8.GetBytes("Hello from ZROS NativeBindingDemo!");
                nativeData = Marshal.AllocHGlobal(msgBytes.Length);
                Marshal.Copy(msgBytes, 0, nativeData, msgBytes.Length);

                // 4-8：创建 owned bytes（传 NULL drop 表示内部深拷贝）
                ZenohNative.z_bytes_from_buf(
                    out var bytes4,
                    nativeData,
                    (UIntPtr)msgBytes.Length,
                    IntPtr.Zero,
                    IntPtr.Zero);
                Console.WriteLine($"  ✅ 创建 bytes payload（{msgBytes.Length} 字节）");

                // 4-9：发布消息（bytes 所有权转移）
                int putResult = ZenohNative.z_publisher_put(loanedPub4, ref bytes4, IntPtr.Zero);
                Console.WriteLine($"  z_publisher_put 返回码 = {putResult} ({ZenohErrorCodes.GetErrorString(putResult)})");
                Console.WriteLine(putResult == ZenohErrorCodes.Z_OK
                    ? "  ✅ 消息发布成功"
                    : $"  ⚠️  消息发布失败（{ZenohErrorCodes.GetErrorString(putResult)}）");

                // 4-10：释放发布者
                ZenohNative.z_publisher_drop(ref publisher4);
                Console.WriteLine("  ✅ 发布者已释放（z_publisher_drop 完成）");
            }
            else
            {
                Console.WriteLine("  ⚠️  发布者声明失败，跳过 put 测试");
            }

            // 4-11：关闭会话
            ZenohNative.z_session_drop(ref session4);
            Console.WriteLine("  ✅ 会话已关闭");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ 异常：{ex.Message}");
    }
    finally
    {
        // 释放非托管内存（bytes_from_buf 已深拷贝，可以安全释放）
        if (nativeData != IntPtr.Zero)
            Marshal.FreeHGlobal(nativeData);
    }
}
Console.WriteLine();

// ────────────────────────────────────────────────────────────────────────────
// 步骤 5：Subscriber 回调测试（仅在真实模式）
// 流程：创建 session → 创建 keyexpr → z_closure_sample → z_declare_subscriber
//        → Thread.Sleep(1000) → z_subscriber_drop → z_session_drop
// ────────────────────────────────────────────────────────────────────────────
Console.WriteLine("【步骤 5】Subscriber 回调测试");
if (isSimulated)
{
    Console.WriteLine("  [跳过] 模拟模式");
}
else
{
    try
    {
        // 5-1：打开会话
        ZenohNative.z_config_default(out var config5);
        int r5 = ZenohNative.z_open(out var session5, ref config5, IntPtr.Zero);
        if (r5 != ZenohErrorCodes.Z_OK)
        {
            ZenohNative.z_config_drop(ref config5);
            Console.WriteLine($"  ❌ 无法打开会话，跳过 Subscriber 测试");
        }
        else
        {
            Console.WriteLine("  ✅ 会话打开成功");

            // 5-2：创建 key expression
            const string Topic5 = "demo/native/test";
            ZenohNative.z_view_keyexpr_from_str_unchecked(out var ke5, Topic5);
            Console.WriteLine($"  ✅ 创建 key expression：\"{Topic5}\"");

            // 5-3：定义消息回调 delegate（使用顶层声明的 SampleCallDelegate 类型）
            //      回调签名：void(const z_loaned_sample_t*, void*)
            SampleCallDelegate onSample = (sample, ctx) =>
            {
                // 在回调中打印接收提示（注意：此处不能进行复杂的 .NET 操作）
                Console.WriteLine("  📩 收到消息（回调触发）！sample 指针 = 0x" + sample.ToString("X16"));
            };

            // 5-4：获取函数指针（GCHandle 隐式由 Marshal 管理，delegate 本身保持引用）
            IntPtr callFp = Marshal.GetFunctionPointerForDelegate(onSample);

            // 5-5：初始化闭包结构体
            ZenohNative.z_closure_sample(out var closure5, callFp, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine("  ✅ 消息回调闭包创建成功");

            // 5-6：借出 session 和 keyexpr 指针
            IntPtr loanedSession5 = ZenohNative.z_session_loan(ref session5);
            IntPtr loanedKe5     = ZenohNative.z_view_keyexpr_loan(ref ke5);

            // 5-7：声明订阅者（closure 所有权转移）
            int subResult = ZenohNative.z_declare_subscriber(
                loanedSession5,
                out var subscriber5,
                loanedKe5,
                ref closure5,
                IntPtr.Zero);
            Console.WriteLine($"  z_declare_subscriber 返回码 = {subResult} ({ZenohErrorCodes.GetErrorString(subResult)})");

            if (subResult == ZenohErrorCodes.Z_OK)
            {
                Console.WriteLine("  ✅ 订阅者声明成功，等待 1 秒接收消息...");
                // 5-8：等待消息到达
                Thread.Sleep(1000);
                Console.WriteLine("  ✅ 等待完成");

                // 5-9：释放订阅者
                ZenohNative.z_subscriber_drop(ref subscriber5);
                Console.WriteLine("  ✅ 订阅者已释放（z_subscriber_drop 完成）");
            }
            else
            {
                // 声明失败时需手动释放未使用的闭包
                ZenohNative.z_closure_sample_drop(ref closure5);
                Console.WriteLine("  ⚠️  订阅者声明失败，已释放闭包");
            }

            // 5-10：关闭会话
            ZenohNative.z_session_drop(ref session5);
            Console.WriteLine("  ✅ 会话已关闭");

            // 保持对 onSample delegate 的引用，防止在 Sleep 期间被 GC 回收
            GC.KeepAlive(onSample);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ 异常：{ex.Message}");
    }
}
Console.WriteLine();

// ────────────────────────────────────────────────────────────────────────────
// 步骤 6：错误码表打印
// 遍历 ZenohErrorCodes 中所有已定义的错误码，打印其数值和中文描述。
// ────────────────────────────────────────────────────────────────────────────
Console.WriteLine("【步骤 6】zenoh-c 1.x 错误码对照表");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

// 所有已定义的错误码（按数值从大到小排列）
(int code, string name)[] errorCodes =
{
    (ZenohErrorCodes.Z_OK,             "Z_OK"),
    (ZenohErrorCodes.Z_EINVAL,         "Z_EINVAL"),
    (ZenohErrorCodes.Z_EPARSE,         "Z_EPARSE"),
    (ZenohErrorCodes.Z_EIO,            "Z_EIO"),
    (ZenohErrorCodes.Z_ENETWORK,       "Z_ENETWORK"),
    (ZenohErrorCodes.Z_ENULL,          "Z_ENULL"),
    (ZenohErrorCodes.Z_EUNAVAILABLE,   "Z_EUNAVAILABLE"),
    (ZenohErrorCodes.Z_EDESERIALIZE,   "Z_EDESERIALIZE"),
    (ZenohErrorCodes.Z_ESESSION_CLOSED,"Z_ESESSION_CLOSED"),
    (ZenohErrorCodes.Z_EUTF8,          "Z_EUTF8"),
    (ZenohErrorCodes.Z_EGENERIC,       "Z_EGENERIC"),
};

foreach (var (code, name) in errorCodes)
{
    Console.WriteLine($"  [{code,5}] {name,-22}: {ZenohErrorCodes.GetErrorString(code)}");
}
Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine();

// ────────────────────────────────────────────────────────────────────────────
// 步骤 7：Bytes + Slice 读取测试（仅在真实模式）
// 流程：准备测试数据 → z_bytes_from_buf → z_bytes_to_slice → z_slice_loan
//        → z_slice_data / z_slice_len → 读回数据验证 → z_slice_drop → 释放内存
// ────────────────────────────────────────────────────────────────────────────
Console.WriteLine("【步骤 7】Bytes + Slice 读取测试");
if (isSimulated)
{
    Console.WriteLine("  [跳过] 模拟模式");
}
else
{
    IntPtr nativeData7 = IntPtr.Zero;
    try
    {
        // 7-1：准备测试数据
        byte[] testData = Encoding.UTF8.GetBytes("ZROS 测试数据");
        Console.WriteLine($"  原始测试数据：\"{Encoding.UTF8.GetString(testData)}\"（{testData.Length} 字节）");

        // 7-2：分配非托管内存并复制数据
        nativeData7 = Marshal.AllocHGlobal(testData.Length);
        Marshal.Copy(testData, 0, nativeData7, testData.Length);
        Console.WriteLine("  ✅ 非托管内存分配完成");

        // 7-3：创建 owned bytes（内部深拷贝）
        ZenohNative.z_bytes_from_buf(
            out var bytes7,
            nativeData7,
            (UIntPtr)testData.Length,
            IntPtr.Zero,
            IntPtr.Zero);
        Console.WriteLine("  ✅ z_bytes_from_buf 完成（内部深拷贝）");

        // 7-4：此时可以安全释放原始非托管内存（bytes 已内部复制）
        Marshal.FreeHGlobal(nativeData7);
        nativeData7 = IntPtr.Zero;
        Console.WriteLine("  ✅ 原始非托管内存已释放");

        // 7-5：将 bytes 转为 slice 以读取原始内容
        //      z_bytes_to_slice 接受 const z_loaned_bytes_t*（即 bytes 的地址）
        //      由于 z_loaned_bytes_t* 与 z_owned_bytes_t* 内存布局兼容，
        //      直接用 unsafe 取本地变量地址即可（栈变量已固定，无需 fixed 语句）
        int toSliceResult;
        z_owned_slice_t slice7;
        unsafe
        {
            toSliceResult = ZenohNative.z_bytes_to_slice((IntPtr)(&bytes7), out slice7);
        }
        Console.WriteLine($"  z_bytes_to_slice 返回码 = {toSliceResult} ({ZenohErrorCodes.GetErrorString(toSliceResult)})");

        if (toSliceResult == ZenohErrorCodes.Z_OK)
        {
            Console.WriteLine("  ✅ bytes → slice 转换成功");

            // 7-6：借出 slice 指针
            IntPtr loanedSlice7 = ZenohNative.z_slice_loan(ref slice7);

            // 7-7：读取数据指针和长度
            IntPtr dataPtr7 = ZenohNative.z_slice_data(loanedSlice7);
            UIntPtr dataLen7 = ZenohNative.z_slice_len(loanedSlice7);
            int len7 = (int)(uint)dataLen7;
            Console.WriteLine($"  z_slice_data = 0x{dataPtr7:X16}，z_slice_len = {len7} 字节");

            // 7-8：读回字节数据并验证
            byte[] readBack7 = new byte[len7];
            Marshal.Copy(dataPtr7, readBack7, 0, len7);
            string readBackStr = Encoding.UTF8.GetString(readBack7);

            bool match7 = readBack7.Length == testData.Length;
            if (match7)
            {
                for (int i = 0; i < testData.Length; i++)
                {
                    if (readBack7[i] != testData[i]) { match7 = false; break; }
                }
            }

            Console.WriteLine($"  读回数据：\"{readBackStr}\"");
            Console.WriteLine(match7
                ? "  ✅ 数据验证通过：读回内容与原始数据完全一致"
                : "  ❌ 数据验证失败：读回内容与原始数据不一致");

            // 7-9：释放 slice
            ZenohNative.z_slice_drop(ref slice7);
            Console.WriteLine("  ✅ slice 已释放（z_slice_drop 完成）");
        }
        else
        {
            // z_bytes_to_slice 失败时 bytes 未被消耗，手动释放
            ZenohNative.z_bytes_drop(ref bytes7);
            Console.WriteLine("  ⚠️  bytes → slice 转换失败，已释放 bytes");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ 异常：{ex.Message}");
    }
    finally
    {
        if (nativeData7 != IntPtr.Zero)
            Marshal.FreeHGlobal(nativeData7);
    }
}
Console.WriteLine();

// ────────────────────────────────────────────────────────────────────────────
// 程序结束
// ────────────────────────────────────────────────────────────────────────────
Console.WriteLine("=== ZROS P/Invoke 原生绑定验证完成 ===");

// ============================================================================
// 辅助函数：验证 struct 大小
// ============================================================================

/// <summary>
/// 打印并验证指定 struct 类型的实际字节大小是否与期望值一致。
/// </summary>
/// <typeparam name="T">要验证的 struct 类型（必须是值类型）。</typeparam>
/// <param name="name">struct 的显示名称。</param>
/// <param name="expected">期望的字节大小（来自 ZenohAbiSizes）。</param>
/// <param name="note">备注（"已确认" 或 "估算上界"）。</param>
static void VerifyStructSize<T>(string name, int expected, string note) where T : struct
{
    int actual = Marshal.SizeOf<T>();
    bool ok = actual == expected;
    string status = ok ? "✅" : "❌";
    Console.WriteLine($"  {name,-35} {actual,8} {expected,8}  {status}    {note}");
}

// ============================================================================
// 顶层类型声明：Subscriber 回调 Delegate
// 必须声明在顶层（非嵌套在语句块内），以便使用 UnmanagedFunctionPointer 特性。
// ============================================================================

/// <summary>
/// zenoh-c 订阅者回调函数的 delegate 类型。
/// 对应 C 签名：void (*call)(const z_loaned_sample_t*, void*)
/// 使用 [UnmanagedFunctionPointer] 确保调用约定与 Cdecl 一致。
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void SampleCallDelegate(IntPtr sample, IntPtr ctx);
