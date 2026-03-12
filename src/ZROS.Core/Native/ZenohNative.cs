using System;
using System.Runtime.InteropServices;

// ============================================================================================================================
// ZROS.Core.Native — zenoh-c 1.7.2 P/Invoke 原生绑定层
//
// 本文件基于 zenoh-c 1.7.2 的真实 C API（头文件来自
//   https://github.com/eclipse-zenoh/zenoh-c/tree/1.7.2）完整实现所有
// P/Invoke 绑定，供 ZROS 框架在 .NET 8 下调用 zenoh-c 原生库使用。
//
// ──────────────────────────────────────────────────────────────────────
// v0.x → v1.x 关键 ABI 差异：
//   1. 所有 owned 类型（session、publisher、subscriber 等）均为
//      不透明大字节数组（opaque struct），大小由 zenoh_opaque.h 定义。
//   2. 所有权模型：owned → loaned（借用）→ moved（所有权转移）
//      - z_*_loan()     从 owned 借出 const loaned 指针（IntPtr）
//      - z_*_loan_mut() 从 owned 借出可变 loaned 指针（IntPtr）
//      - z_move(x)      将 owned 转为 moved（C 宏，P/Invoke 中用 ref 传递）
//   3. z_open 新签名：(session*, moved_config*, open_options*) — 三参数
//   4. 关闭会话改为 z_session_drop，z_close 用于优雅关闭（通知网络）
//   5. Publisher.put 接受 z_owned_bytes_t 而非裸 (buf, len)
//   6. 订阅者回调使用闭包结构体 z_owned_closure_sample_t（3 个函数指针）
//
// ──────────────────────────────────────────────────────────────────────
// struct 大小来源（zenoh-c 1.7.2 x86_64-pc-windows-msvc zenoh_opaque.h）：
//   已确认（confirmed）= 直接来自官方头文件字节数组长度
//   估算值（estimated） = 合理估算，需对照实际 zenoh_opaque.h 验证
//
// DLL 放置（Windows x64）：
//   将 zenohc.dll（来自 zenoh-c-1.7.2-x86_64-pc-windows-msvc-standalone）
//   放到可执行文件旁，或放在 src/ZROS.Core/native/win-x64/native/zenohc.dll
//   以便 NuGet 打包。
// ============================================================================================================================

namespace ZROS.Core.Native
{
    // ================================================================
    // 一、ABI 字节大小常量（ZenohAbiSizes）
    // ================================================================

    /// <summary>
    /// zenoh-c 1.7.2 x86_64-pc-windows-msvc 各 opaque struct 的字节大小常量。
    /// <para>
    /// 用途：
    /// <list type="bullet">
    ///   <item>在 struct 定义中通过 <see cref="StructLayoutAttribute.Size"/> 固定内存布局</item>
    ///   <item>在运行时用 <see cref="Marshal.SizeOf{T}()"/> 对比，确认 ABI 无误</item>
    /// </list>
    /// </para>
    /// <para>
    /// 验证方法：
    /// <code>
    ///   dotnet test tests/ZROS.Tests --filter "Category=ZenohInterop"
    /// </code>
    /// 或运行 samples/ZROS.NativeBindingDemo 的步骤 1。
    /// </para>
    /// </summary>
    public static class ZenohAbiSizes
    {
        // ── 会话与配置 ───────────────────────────────────────────────
        /// <summary>z_owned_session_t 大小（已确认）：单指针 Arc，8 字节。</summary>
        public const int SessionBytes     = 8;

        /// <summary>z_owned_config_t 大小（已确认）：单指针，8 字节。</summary>
        public const int ConfigBytes      = 8;

        // ── 发布者 ───────────────────────────────────────────────────
        /// <summary>z_owned_publisher_t 大小（已确认）：Rust 内联结构体，112 字节。</summary>
        public const int PublisherBytes   = 112;

        // ── 订阅者与可查询对象 ────────────────────────────────────────
        /// <summary>z_owned_subscriber_t 大小（已确认）：单指针句柄，8 字节。</summary>
        public const int SubscriberBytes  = 8;

        /// <summary>z_owned_queryable_t 大小（已确认）：单指针句柄，8 字节。</summary>
        public const int QueryableBytes   = 8;

        // ── Key Expression ───────────────────────────────────────────
        /// <summary>
        /// z_view_keyexpr_t 大小（估算值）：含字符串指针 + 长度，32 字节为安全上界。
        /// 请对照实际 zenoh_opaque.h 验证。
        /// </summary>
        public const int ViewKeyexprBytes = 32;

        // ── 字节载荷与切片 ────────────────────────────────────────────
        /// <summary>
        /// z_owned_bytes_t 大小（估算值）：Rust Vec-like 结构体 + 额外字段，48 字节。
        /// 请对照实际 zenoh_opaque.h 验证。
        /// </summary>
        public const int OwnedBytesBytes  = 48;

        /// <summary>z_owned_slice_t 大小（已确认）：data 指针 + len，16 字节（x64）。</summary>
        public const int SliceBytes       = 16;

        // ── 闭包 ─────────────────────────────────────────────────────
        /// <summary>z_owned_closure_sample_t 大小（已确认）：3 × 8 字节函数指针，共 24 字节。</summary>
        public const int ClosureSampleBytes = 24;

        /// <summary>z_owned_closure_query_t 大小（已确认）：3 × 8 字节函数指针，共 24 字节。</summary>
        public const int ClosureQueryBytes  = 24;
    }

    // ================================================================
    // 二、Opaque Struct 定义（不透明字节数组结构体）
    // ================================================================
    // 所有 owned 类型均使用 [StructLayout(LayoutKind.Sequential, Size = N)]
    // 定义为空结构体，通过 Size 字段固定字节大小，与 C 端内存布局对齐。
    // z_moved_*_t 类型与对应 owned 类型内存布局完全一致，
    // 在 P/Invoke 中统一用 ref z_owned_*_t 传递（表示所有权转移）。

    /// <summary>
    /// 持有 Zenoh 会话的 owned 类型。
    /// 大小：8 字节（已确认）—— Rust Arc 的单指针。
    /// 关闭方式：z_session_drop(ref session) 或 z_close(loaned_session, IntPtr.Zero)。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.SessionBytes)]
    public struct z_owned_session_t { }

    /// <summary>
    /// 持有 Zenoh 配置的 owned 类型。
    /// 大小：8 字节（已确认）—— 单指针。
    /// 使用流程：z_config_default → z_open（消耗所有权）或 z_config_drop（手动释放）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.ConfigBytes)]
    public struct z_owned_config_t { }

    /// <summary>
    /// 持有发布者的 owned 类型。
    /// 大小：112 字节（已确认）—— Rust 内联结构体。
    /// 释放方式：z_publisher_drop(ref publisher)。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.PublisherBytes)]
    public struct z_owned_publisher_t { }

    /// <summary>
    /// 持有订阅者的 owned 类型。
    /// 大小：8 字节（已确认）—— 单指针句柄。
    /// 释放方式：z_subscriber_drop(ref subscriber)。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.SubscriberBytes)]
    public struct z_owned_subscriber_t { }

    /// <summary>
    /// 持有可查询对象（queryable）的 owned 类型。
    /// 大小：8 字节（已确认）—— 单指针句柄。
    /// 释放方式：z_queryable_drop(ref queryable)。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.QueryableBytes)]
    public struct z_owned_queryable_t { }

    /// <summary>
    /// Key Expression 的视图类型（不拥有内存，借用字符串）。
    /// 大小：32 字节（估算安全上界）—— 含字符串指针 + 长度字段。
    /// 注意：字符串本身的生命周期必须长于此视图。
    /// 创建方式：z_view_keyexpr_from_str 或 z_view_keyexpr_from_str_unchecked。
    /// 借出方式：z_view_keyexpr_loan(ref ke) → IntPtr（传给 z_declare_* 函数）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.ViewKeyexprBytes)]
    public struct z_view_keyexpr_t { }

    /// <summary>
    /// 持有字节载荷的 owned 类型（Zenoh 消息体）。
    /// 大小：48 字节（估算值）—— Rust Vec-like 结构体 + 额外字段。
    /// 创建方式：z_bytes_from_buf 或 z_bytes_copy_from_buf。
    /// 释放方式：z_bytes_drop(ref bytes)（若未通过 z_publisher_put 消耗）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.OwnedBytesBytes)]
    public struct z_owned_bytes_t { }

    /// <summary>
    /// 持有字节切片的 owned 类型（用于读取消息载荷内容）。
    /// 大小：16 字节（已确认）—— data 指针（8 字节）+ len（8 字节，x64）。
    /// 使用方式：z_bytes_to_slice(bytes_ptr, out slice) 后通过 z_slice_data/z_slice_len 读取。
    /// 释放方式：z_slice_drop(ref slice)。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = ZenohAbiSizes.SliceBytes)]
    public struct z_owned_slice_t { }

    // ================================================================
    // 三、闭包结构体（含具体字段）
    // ================================================================

    /// <summary>
    /// 订阅者消息回调闭包（用于 z_declare_subscriber）。
    /// 大小：24 字节（已确认）—— 3 × 8 字节函数指针（x64）。
    /// <para>
    /// 字段含义：
    /// <list type="bullet">
    ///   <item><see cref="Context"/>：用户自定义上下文指针，传给 Call/Drop 回调</item>
    ///   <item><see cref="Call"/>：消息到达时的回调，签名 void(z_loaned_sample_t*, void*)</item>
    ///   <item><see cref="Drop"/>：闭包被释放时的清理回调，签名 void(void*)</item>
    /// </list>
    /// </para>
    /// 使用方式：调用 z_closure_sample(out closure, callFp, dropFp, contextPtr) 填充。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_closure_sample_t
    {
        /// <summary>用户自定义上下文指针，将传递给 Call 和 Drop 回调。</summary>
        public IntPtr Context;

        /// <summary>
        /// 消息到达时触发的回调函数指针。
        /// C 签名：void (*call)(const z_loaned_sample_t*, void*)
        /// </summary>
        public IntPtr Call;

        /// <summary>
        /// 闭包被释放时触发的清理函数指针（可为 IntPtr.Zero）。
        /// C 签名：void (*drop)(void*)
        /// </summary>
        public IntPtr Drop;
    }

    /// <summary>
    /// 可查询对象查询回调闭包（用于 z_declare_queryable）。
    /// 大小：24 字节（已确认）—— 3 × 8 字节函数指针（x64）。
    /// <para>
    /// 字段含义与 <see cref="z_owned_closure_sample_t"/> 相同，
    /// 但 Call 的签名为 void(z_loaned_query_t*, void*)。
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct z_owned_closure_query_t
    {
        /// <summary>用户自定义上下文指针，将传递给 Call 和 Drop 回调。</summary>
        public IntPtr Context;

        /// <summary>
        /// 查询到达时触发的回调函数指针。
        /// C 签名：void (*call)(const z_loaned_query_t*, void*)
        /// </summary>
        public IntPtr Call;

        /// <summary>
        /// 闭包被释放时触发的清理函数指针（可为 IntPtr.Zero）。
        /// C 签名：void (*drop)(void*)
        /// </summary>
        public IntPtr Drop;
    }

    // ================================================================
    // 四、错误码常量（ZenohErrorCodes）
    // ================================================================

    /// <summary>
    /// zenoh-c v1.x 全部错误码常量（来自 zenoh_concrete.h）。
    /// <para>
    /// ⚠️ 与 v0.x 的差异：Z_ESESSION_CLOSED 由 -5 改为 -8，新增 Z_EUTF8 = -9。
    /// </para>
    /// </summary>
    public static class ZenohErrorCodes
    {
        /// <summary>操作成功（0）。</summary>
        public const int Z_OK             = 0;

        /// <summary>无效参数（-1）：传入的参数值非法。</summary>
        public const int Z_EINVAL         = -1;

        /// <summary>解析错误（-2）：配置或表达式解析失败。</summary>
        public const int Z_EPARSE         = -2;

        /// <summary>I/O 错误（-3）：底层 I/O 操作失败。</summary>
        public const int Z_EIO            = -3;

        /// <summary>网络错误（-4）：网络连接或传输失败。</summary>
        public const int Z_ENETWORK       = -4;

        /// <summary>空指针错误（-5）：必填指针为 NULL。</summary>
        public const int Z_ENULL          = -5;

        /// <summary>不可用（-6）：请求的功能或资源不可用。</summary>
        public const int Z_EUNAVAILABLE   = -6;

        /// <summary>反序列化错误（-7）：消息载荷无法被解析。</summary>
        public const int Z_EDESERIALIZE   = -7;

        /// <summary>会话已关闭（-8）：对已关闭的会话执行操作。</summary>
        public const int Z_ESESSION_CLOSED = -8;

        /// <summary>UTF-8 编码错误（-9）：字符串不是合法的 UTF-8 序列。</summary>
        public const int Z_EUTF8          = -9;

        /// <summary>通用错误（-128 = INT8_MIN）：未分类的内部错误。</summary>
        public const int Z_EGENERIC       = -128;

        /// <summary>
        /// 将错误码转换为人类可读的中文描述字符串。
        /// </summary>
        /// <param name="code">zenoh-c 返回的错误码（通常为负数或 0）。</param>
        /// <returns>对应的中文描述，未知码时返回 "未知错误 (code)" 格式字符串。</returns>
        public static string GetErrorString(int code) => code switch
        {
            Z_OK              => "成功 (Z_OK)",
            Z_EINVAL          => "无效参数 (Z_EINVAL)",
            Z_EPARSE          => "解析错误 (Z_EPARSE)",
            Z_EIO             => "I/O 错误 (Z_EIO)",
            Z_ENETWORK        => "网络错误 (Z_ENETWORK)",
            Z_ENULL           => "空指针错误 (Z_ENULL)",
            Z_EUNAVAILABLE    => "不可用 (Z_EUNAVAILABLE)",
            Z_EDESERIALIZE    => "反序列化错误 (Z_EDESERIALIZE)",
            Z_ESESSION_CLOSED => "会话已关闭 (Z_ESESSION_CLOSED)",
            Z_EUTF8           => "UTF-8 编码错误 (Z_EUTF8)",
            Z_EGENERIC        => "通用错误 (Z_EGENERIC)",
            _                 => $"未知错误 ({code})"
        };
    }

    // ================================================================
    // 五、P/Invoke 原生绑定（ZenohNative）
    // ================================================================

    /// <summary>
    /// zenoh-c 1.7.2 原生库的完整 P/Invoke 绑定。
    /// <para>
    /// <b>参数类型映射规则：</b>
    /// <list type="bullet">
    ///   <item>z_loaned_*_t* —— 由 z_*_loan() 返回的借用指针，使用 <see cref="IntPtr"/></item>
    ///   <item>z_owned_*_t*（out 参数）—— 使用 out z_owned_*_t</item>
    ///   <item>z_owned_*_t*（in/moved 参数）—— 使用 ref z_owned_*_t（表示所有权转移）</item>
    ///   <item>const char* —— 使用 [MarshalAs(UnmanagedType.LPStr)] string</item>
    ///   <item>const uint8_t* —— 使用 IntPtr（配合 Marshal.AllocHGlobal / fixed）</item>
    ///   <item>size_t —— 使用 UIntPtr</item>
    ///   <item>void* —— 使用 IntPtr</item>
    ///   <item>返回 const z_loaned_*_t* 的函数 —— 返回 IntPtr</item>
    ///   <item>options 结构体指针 —— 使用 IntPtr（传 IntPtr.Zero 使用默认值）</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>DLL 名称：</b>"zenohc"（Windows: zenohc.dll，Linux: libzenohc.so，macOS: libzenohc.dylib）。
    /// </para>
    /// </summary>
    public static class ZenohNative
    {
        /// <summary>原生库名称（不含平台前后缀，.NET 运行时自动处理）。</summary>
        private const string LibName = "zenohc";

        // ────────────────────────────────────────────────────────────
        // 5.1 配置（Config）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 使用默认配置初始化一个 owned config 对象。
        /// <para>C 签名：<c>void z_config_default(z_owned_config_t* config)</c></para>
        /// <para>
        /// 使用场景：调用 z_open 之前必须先通过此函数创建配置对象。
        /// 若 z_open 成功，config 的所有权将被转移（consumed）；
        /// 若 z_open 失败，需手动调用 z_config_drop 释放。
        /// </para>
        /// </summary>
        /// <param name="config">输出参数，接收创建的默认配置对象。</param>
        [DllImport(LibName, EntryPoint = "z_config_default", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_config_default(out z_owned_config_t config);

        /// <summary>
        /// 释放一个未被 z_open 消耗的 owned config 对象。
        /// <para>C 签名：<c>void z_config_drop(z_moved_config_t* config)</c></para>
        /// <para>
        /// 注意：z_moved_config_t* 与 z_owned_config_t* 内存布局完全一致，
        /// 因此 P/Invoke 中使用 ref z_owned_config_t 传递。
        /// </para>
        /// </summary>
        /// <param name="config">要释放的 owned config 对象（所有权转移，调用后不可再使用）。</param>
        [DllImport(LibName, EntryPoint = "z_config_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_config_drop(ref z_owned_config_t config);

        // ────────────────────────────────────────────────────────────
        // 5.2 会话（Session）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 打开一个 Zenoh 会话（v1.x 三参数版本）。
        /// <para>C 签名：<c>z_result_t z_open(z_owned_session_t* session, z_moved_config_t* config, const z_open_options_t* options)</c></para>
        /// <para>
        /// 与 v0.x 的差异：v1.x 中 config 必须通过 z_config_default 单独创建，
        /// 不再由 z_open 内部构造；options 传 IntPtr.Zero 使用默认连接参数。
        /// </para>
        /// <para>
        /// 成功时：config 所有权被转移（consumed），session 被填充，返回 Z_OK (0)。
        /// 失败时：config 所有权未转移，调用方需手动调用 z_config_drop 释放。
        /// </para>
        /// </summary>
        /// <param name="session">输出参数，接收打开的会话对象。</param>
        /// <param name="config">移动参数，配置对象的所有权将被消耗。</param>
        /// <param name="options">选项结构体指针，传 IntPtr.Zero 使用默认值。</param>
        /// <returns>Z_OK (0) 表示成功，负数错误码表示失败（参见 ZenohErrorCodes）。</returns>
        [DllImport(LibName, EntryPoint = "z_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_open(
            out z_owned_session_t session,
            ref z_owned_config_t  config,
            IntPtr                options);

        /// <summary>
        /// 销毁（关闭）一个 owned session 对象，释放相关资源。
        /// <para>C 签名：<c>void z_session_drop(z_moved_session_t* session)</c></para>
        /// <para>
        /// 等同于 C 中的 z_drop(z_move(session))。
        /// 此函数直接关闭，不向网络发送关闭通知；
        /// 若需优雅关闭（通知对端），请先调用 z_close，再调用此函数。
        /// </para>
        /// </summary>
        /// <param name="session">要关闭的 owned session（所有权转移，调用后不可再使用）。</param>
        [DllImport(LibName, EntryPoint = "z_session_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_session_drop(ref z_owned_session_t session);

        /// <summary>
        /// 借出一个只读的 loaned session 指针（不转移所有权）。
        /// <para>C 签名：<c>const z_loaned_session_t* z_session_loan(const z_owned_session_t* session)</c></para>
        /// <para>
        /// 返回的 IntPtr 指向与 session 相同的内存，生命周期受 session 约束。
        /// 将此 IntPtr 传递给 z_declare_publisher、z_declare_subscriber 等函数。
        /// </para>
        /// </summary>
        /// <param name="session">源 owned session 对象（不消耗，调用后仍可使用）。</param>
        /// <returns>借用的只读 session 指针（IntPtr），不可超过 session 生命周期使用。</returns>
        [DllImport(LibName, EntryPoint = "z_session_loan", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_session_loan(ref z_owned_session_t session);

        /// <summary>
        /// 借出一个可变的 loaned session 指针（不转移所有权）。
        /// <para>C 签名：<c>z_loaned_session_t* z_session_loan_mut(z_owned_session_t* session)</c></para>
        /// <para>
        /// 与 z_session_loan 类似，但返回可变指针，供需要修改 session 的函数使用。
        /// 例如 z_close 需要可变 session 指针。
        /// </para>
        /// </summary>
        /// <param name="session">源 owned session 对象（不消耗）。</param>
        /// <returns>借用的可变 session 指针（IntPtr）。</returns>
        [DllImport(LibName, EntryPoint = "z_session_loan_mut", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_session_loan_mut(ref z_owned_session_t session);

        /// <summary>
        /// 优雅关闭 Zenoh 会话（向网络发送关闭通知）。
        /// <para>C 签名：<c>z_result_t z_close(z_loaned_session_t* session, const z_close_options_t* options)</c></para>
        /// <para>
        /// 与 z_session_drop 的区别：z_close 会向 Zenoh 网络广播关闭消息，
        /// 让订阅者和查询者知道此会话已下线；z_session_drop 则直接释放资源。
        /// 通常先调用 z_close，再调用 z_session_drop。
        /// </para>
        /// </summary>
        /// <param name="session">可变借用的 session 指针（由 z_session_loan_mut 获取）。</param>
        /// <param name="options">关闭选项指针，传 IntPtr.Zero 使用默认值。</param>
        /// <returns>Z_OK (0) 表示成功，负数错误码表示失败。</returns>
        [DllImport(LibName, EntryPoint = "z_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_close(IntPtr session, IntPtr options);

        // ────────────────────────────────────────────────────────────
        // 5.3 Key Expression（键表达式）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 从字符串创建一个 view keyexpr（会验证合法性，速度稍慢）。
        /// <para>C 签名：<c>z_result_t z_view_keyexpr_from_str(z_view_keyexpr_t* keyexpr, const char* expr)</c></para>
        /// <para>
        /// 注意：<paramref name="expr"/> 字符串的生命周期必须长于 <paramref name="keyexpr"/>，
        /// 因为 view 直接借用字符串内存，不进行复制。
        /// </para>
        /// </summary>
        /// <param name="keyexpr">输出参数，接收创建的 view keyexpr。</param>
        /// <param name="expr">键表达式字符串（ANSI 编码，不能为 null）。</param>
        /// <returns>Z_OK (0) 表示合法，负数表示字符串不是合法的 key expression。</returns>
        [DllImport(LibName, EntryPoint = "z_view_keyexpr_from_str",
            CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int z_view_keyexpr_from_str(
            out z_view_keyexpr_t                      keyexpr,
            [MarshalAs(UnmanagedType.LPStr)] string   expr);

        /// <summary>
        /// 从字符串创建一个 view keyexpr（不验证合法性，速度更快）。
        /// <para>C 签名：<c>void z_view_keyexpr_from_str_unchecked(z_view_keyexpr_t* keyexpr, const char* expr)</c></para>
        /// <para>
        /// 适合在确知字符串合法时使用（如硬编码的 topic 名称），
        /// 避免 z_view_keyexpr_from_str 的合法性检查开销。
        /// 同样要求字符串生命周期长于 keyexpr。
        /// </para>
        /// </summary>
        /// <param name="keyexpr">输出参数，接收创建的 view keyexpr。</param>
        /// <param name="expr">键表达式字符串（ANSI 编码，调用方保证合法性）。</param>
        [DllImport(LibName, EntryPoint = "z_view_keyexpr_from_str_unchecked",
            CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void z_view_keyexpr_from_str_unchecked(
            out z_view_keyexpr_t                      keyexpr,
            [MarshalAs(UnmanagedType.LPStr)] string   expr);

        /// <summary>
        /// 从 view keyexpr 借出只读的 loaned keyexpr 指针。
        /// <para>C 签名：<c>const z_loaned_keyexpr_t* z_view_keyexpr_loan(const z_view_keyexpr_t* keyexpr)</c></para>
        /// <para>
        /// 返回的 IntPtr 传递给 z_declare_publisher、z_declare_subscriber 等函数。
        /// 不转移所有权，生命周期受 keyexpr 约束。
        /// </para>
        /// </summary>
        /// <param name="keyexpr">源 view keyexpr（不消耗）。</param>
        /// <returns>借用的只读 keyexpr 指针（IntPtr）。</returns>
        [DllImport(LibName, EntryPoint = "z_view_keyexpr_loan", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_view_keyexpr_loan(ref z_view_keyexpr_t keyexpr);

        // ────────────────────────────────────────────────────────────
        // 5.4 发布者（Publisher）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 在指定键表达式上声明发布者。
        /// <para>C 签名：<c>z_result_t z_declare_publisher(const z_loaned_session_t* session, z_owned_publisher_t* publisher, const z_loaned_keyexpr_t* keyexpr, const z_publisher_options_t* options)</c></para>
        /// <para>
        /// 参数 session 和 keyexpr 均为借用指针（由 z_session_loan / z_view_keyexpr_loan 获取），
        /// 不转移所有权。options 传 IntPtr.Zero 使用默认发布者选项。
        /// </para>
        /// </summary>
        /// <param name="session">借用的 session 指针（由 z_session_loan 获取）。</param>
        /// <param name="publisher">输出参数，接收声明的发布者对象。</param>
        /// <param name="keyexpr">借用的 keyexpr 指针（由 z_view_keyexpr_loan 获取）。</param>
        /// <param name="options">发布者选项指针，传 IntPtr.Zero 使用默认值。</param>
        /// <returns>Z_OK (0) 表示成功，负数错误码表示失败。</returns>
        [DllImport(LibName, EntryPoint = "z_declare_publisher", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_publisher(
            IntPtr                session,
            out z_owned_publisher_t publisher,
            IntPtr                keyexpr,
            IntPtr                options);

        /// <summary>
        /// 释放（撤销声明）一个 owned publisher 对象。
        /// <para>C 签名：<c>void z_publisher_drop(z_moved_publisher_t* publisher)</c></para>
        /// </summary>
        /// <param name="publisher">要释放的 owned publisher（所有权转移，调用后不可再使用）。</param>
        [DllImport(LibName, EntryPoint = "z_publisher_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_publisher_drop(ref z_owned_publisher_t publisher);

        /// <summary>
        /// 通过发布者发布消息载荷（消耗 bytes 所有权）。
        /// <para>C 签名：<c>z_result_t z_publisher_put(const z_loaned_publisher_t* publisher, z_moved_bytes_t* payload, const z_publisher_put_options_t* options)</c></para>
        /// <para>
        /// 成功后 <paramref name="payload"/> 的所有权被转移，不可再使用。
        /// options 传 IntPtr.Zero 使用默认发布选项（无附件、无时间戳等）。
        /// </para>
        /// </summary>
        /// <param name="publisher">借用的只读 publisher 指针（由 z_publisher_loan 获取）。</param>
        /// <param name="payload">要发布的字节载荷（所有权转移）。</param>
        /// <param name="options">发布选项指针，传 IntPtr.Zero 使用默认值。</param>
        /// <returns>Z_OK (0) 表示成功，负数错误码表示失败。</returns>
        [DllImport(LibName, EntryPoint = "z_publisher_put", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_publisher_put(
            IntPtr               publisher,
            ref z_owned_bytes_t  payload,
            IntPtr               options);

        /// <summary>
        /// 借出只读的 loaned publisher 指针。
        /// <para>C 签名：<c>const z_loaned_publisher_t* z_publisher_loan(const z_owned_publisher_t* publisher)</c></para>
        /// </summary>
        /// <param name="publisher">源 owned publisher（不消耗）。</param>
        /// <returns>借用的只读 publisher 指针（IntPtr），传给 z_publisher_put 等函数。</returns>
        [DllImport(LibName, EntryPoint = "z_publisher_loan", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_publisher_loan(ref z_owned_publisher_t publisher);

        /// <summary>
        /// 借出可变的 loaned publisher 指针。
        /// <para>C 签名：<c>z_loaned_publisher_t* z_publisher_loan_mut(z_owned_publisher_t* publisher)</c></para>
        /// </summary>
        /// <param name="publisher">源 owned publisher（不消耗）。</param>
        /// <returns>借用的可变 publisher 指针（IntPtr）。</returns>
        [DllImport(LibName, EntryPoint = "z_publisher_loan_mut", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_publisher_loan_mut(ref z_owned_publisher_t publisher);

        /// <summary>
        /// 初始化发布者选项结构体为默认值。
        /// <para>C 签名：<c>void z_publisher_options_default(z_publisher_options_t* options)</c></para>
        /// <para>
        /// 通常传 IntPtr.Zero 即可使用默认选项，此函数供需要自定义选项时使用。
        /// </para>
        /// </summary>
        /// <param name="options">要初始化的发布者选项结构体指针。</param>
        [DllImport(LibName, EntryPoint = "z_publisher_options_default", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_publisher_options_default(IntPtr options);

        /// <summary>
        /// 初始化发布者 put 选项结构体为默认值。
        /// <para>C 签名：<c>void z_publisher_put_options_default(z_publisher_put_options_t* options)</c></para>
        /// </summary>
        /// <param name="options">要初始化的 put 选项结构体指针。</param>
        [DllImport(LibName, EntryPoint = "z_publisher_put_options_default", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_publisher_put_options_default(IntPtr options);

        // ────────────────────────────────────────────────────────────
        // 5.5 订阅者（Subscriber）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 声明一个订阅者，指定消息到达时的回调闭包。
        /// <para>C 签名：<c>z_result_t z_declare_subscriber(const z_loaned_session_t* session, z_owned_subscriber_t* subscriber, const z_loaned_keyexpr_t* keyexpr, z_moved_closure_sample_t* callback, const z_subscriber_options_t* options)</c></para>
        /// <para>
        /// <paramref name="callback"/> 的所有权被转移，调用后不可再访问其字段。
        /// 回调在消息到达时由 zenoh 运行时在内部线程调用，需注意线程安全。
        /// </para>
        /// </summary>
        /// <param name="session">借用的 session 指针（由 z_session_loan 获取）。</param>
        /// <param name="subscriber">输出参数，接收声明的订阅者对象。</param>
        /// <param name="keyexpr">借用的 keyexpr 指针（由 z_view_keyexpr_loan 获取）。</param>
        /// <param name="callback">移动参数，消息回调闭包（所有权转移）。</param>
        /// <param name="options">订阅者选项指针，传 IntPtr.Zero 使用默认值。</param>
        /// <returns>Z_OK (0) 表示成功，负数错误码表示失败。</returns>
        [DllImport(LibName, EntryPoint = "z_declare_subscriber", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_subscriber(
            IntPtr                         session,
            out z_owned_subscriber_t       subscriber,
            IntPtr                         keyexpr,
            ref z_owned_closure_sample_t   callback,
            IntPtr                         options);

        /// <summary>
        /// 释放（撤销声明）一个 owned subscriber 对象。
        /// <para>C 签名：<c>void z_subscriber_drop(z_moved_subscriber_t* subscriber)</c></para>
        /// </summary>
        /// <param name="subscriber">要释放的 owned subscriber（所有权转移，调用后不可再使用）。</param>
        [DllImport(LibName, EntryPoint = "z_subscriber_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_subscriber_drop(ref z_owned_subscriber_t subscriber);

        // ────────────────────────────────────────────────────────────
        // 5.6 字节载荷（Bytes）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 从原始字节缓冲区创建 owned bytes（带自定义 drop 回调的版本）。
        /// <para>C 签名：<c>void z_bytes_from_buf(z_owned_bytes_t* bytes, const uint8_t* data, size_t len, void(*drop)(void*, void*), void* context)</c></para>
        /// <para>
        /// 当 <paramref name="dropCallback"/> 和 <paramref name="dropContext"/> 均为 IntPtr.Zero 时，
        /// zenoh-c 内部对数据进行深拷贝（最安全的做法）。
        /// 若传入非 NULL 的 drop 回调，则 zenoh 在释放时调用该回调，可实现零拷贝。
        /// </para>
        /// </summary>
        /// <param name="bytes">输出参数，接收创建的 owned bytes 对象。</param>
        /// <param name="data">原始字节数据指针（由 Marshal.AllocHGlobal 或 fixed 获取）。</param>
        /// <param name="len">数据长度（字节数）。</param>
        /// <param name="dropCallback">数据释放回调，签名 void(*)(void*, void*)；传 IntPtr.Zero 表示内部复制。</param>
        /// <param name="dropContext">传给 dropCallback 的上下文指针；无自定义 drop 时传 IntPtr.Zero。</param>
        [DllImport(LibName, EntryPoint = "z_bytes_from_buf", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_bytes_from_buf(
            out z_owned_bytes_t  bytes,
            IntPtr               data,
            UIntPtr              len,
            IntPtr               dropCallback,
            IntPtr               dropContext);

        /// <summary>
        /// 从原始字节缓冲区深拷贝创建 owned bytes（简化版本）。
        /// <para>C 签名：<c>void z_bytes_copy_from_buf(z_owned_bytes_t* bytes, const uint8_t* data, size_t len)</c></para>
        /// <para>
        /// 等同于以 NULL drop 回调调用 z_bytes_from_buf，即总是深拷贝数据。
        /// </para>
        /// </summary>
        /// <param name="bytes">输出参数，接收创建的 owned bytes 对象。</param>
        /// <param name="data">原始字节数据指针。</param>
        /// <param name="len">数据长度（字节数）。</param>
        [DllImport(LibName, EntryPoint = "z_bytes_copy_from_buf", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_bytes_copy_from_buf(
            out z_owned_bytes_t  bytes,
            IntPtr               data,
            UIntPtr              len);

        /// <summary>
        /// 释放一个 owned bytes 对象。
        /// <para>C 签名：<c>void z_bytes_drop(z_moved_bytes_t* bytes)</c></para>
        /// <para>注意：若 bytes 已通过 z_publisher_put 消耗，则无需再调用此函数。</para>
        /// </summary>
        /// <param name="bytes">要释放的 owned bytes（所有权转移）。</param>
        [DllImport(LibName, EntryPoint = "z_bytes_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_bytes_drop(ref z_owned_bytes_t bytes);

        /// <summary>
        /// 将 loaned bytes 转换为 owned slice（用于读取消息载荷的原始字节内容）。
        /// <para>C 签名：<c>z_result_t z_bytes_to_slice(const z_loaned_bytes_t* bytes, z_owned_slice_t* slice)</c></para>
        /// <para>
        /// 成功后通过 z_slice_loan → z_slice_data / z_slice_len 读取内容，
        /// 最后调用 z_slice_drop 释放 slice。
        /// bytes 参数通常来自 z_sample_payload 的返回值。
        /// </para>
        /// </summary>
        /// <param name="bytes">借用的 bytes 指针（如 z_sample_payload 的返回值）。</param>
        /// <param name="slice">输出参数，接收转换后的 owned slice 对象。</param>
        /// <returns>Z_OK (0) 表示成功，负数错误码表示失败。</returns>
        [DllImport(LibName, EntryPoint = "z_bytes_to_slice", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_bytes_to_slice(IntPtr bytes, out z_owned_slice_t slice);

        // ────────────────────────────────────────────────────────────
        // 5.7 字节切片（Slice）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 借出 owned slice 的只读指针（用于读取切片内容）。
        /// <para>C 签名：<c>const z_loaned_slice_t* z_slice_loan(const z_owned_slice_t* slice)</c></para>
        /// </summary>
        /// <param name="slice">源 owned slice（不消耗）。</param>
        /// <returns>借用的只读 slice 指针（IntPtr），传给 z_slice_data / z_slice_len。</returns>
        [DllImport(LibName, EntryPoint = "z_slice_loan", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_slice_loan(ref z_owned_slice_t slice);

        /// <summary>
        /// 获取 loaned slice 内部数据指针（原始字节内存地址）。
        /// <para>C 签名：<c>const uint8_t* z_slice_data(const z_loaned_slice_t* slice)</c></para>
        /// <para>
        /// 返回的 IntPtr 可通过 Marshal.Copy 读取到 byte[] 中：
        /// <code>
        ///   IntPtr dataPtr = ZenohNative.z_slice_data(loanedSlicePtr);
        ///   int len = (int)ZenohNative.z_slice_len(loanedSlicePtr);
        ///   byte[] result = new byte[len];
        ///   Marshal.Copy(dataPtr, result, 0, len);
        /// </code>
        /// </para>
        /// </summary>
        /// <param name="slice">借用的只读 slice 指针（由 z_slice_loan 获取）。</param>
        /// <returns>指向原始字节数据的指针，不拥有所有权。</returns>
        [DllImport(LibName, EntryPoint = "z_slice_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_slice_data(IntPtr slice);

        /// <summary>
        /// 获取 loaned slice 的数据长度（字节数）。
        /// <para>C 签名：<c>size_t z_slice_len(const z_loaned_slice_t* slice)</c></para>
        /// </summary>
        /// <param name="slice">借用的只读 slice 指针（由 z_slice_loan 获取）。</param>
        /// <returns>切片数据的字节长度（size_t，映射为 UIntPtr）。</returns>
        [DllImport(LibName, EntryPoint = "z_slice_len", CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr z_slice_len(IntPtr slice);

        /// <summary>
        /// 释放一个 owned slice 对象。
        /// <para>C 签名：<c>void z_slice_drop(z_moved_slice_t* slice)</c></para>
        /// </summary>
        /// <param name="slice">要释放的 owned slice（所有权转移）。</param>
        [DllImport(LibName, EntryPoint = "z_slice_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_slice_drop(ref z_owned_slice_t slice);

        // ────────────────────────────────────────────────────────────
        // 5.8 消息样本（Sample，用于订阅者回调）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 从订阅者回调收到的 sample 中获取消息载荷的借用指针。
        /// <para>C 签名：<c>const z_loaned_bytes_t* z_sample_payload(const z_loaned_sample_t* sample)</c></para>
        /// <para>
        /// 返回值为借用指针，生命周期受 sample 约束（仅在回调函数执行期间有效）。
        /// 若需在回调外使用数据，应调用 z_bytes_to_slice 转换并复制数据。
        /// </para>
        /// </summary>
        /// <param name="sample">订阅者回调中收到的 loaned sample 指针。</param>
        /// <returns>借用的 bytes 指针（IntPtr），传给 z_bytes_to_slice 等函数。</returns>
        [DllImport(LibName, EntryPoint = "z_sample_payload", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_sample_payload(IntPtr sample);

        /// <summary>
        /// 从订阅者回调收到的 sample 中获取 key expression 的借用指针。
        /// <para>C 签名：<c>const z_loaned_keyexpr_t* z_sample_keyexpr(const z_loaned_sample_t* sample)</c></para>
        /// </summary>
        /// <param name="sample">订阅者回调中收到的 loaned sample 指针。</param>
        /// <returns>借用的 keyexpr 指针（IntPtr）。</returns>
        [DllImport(LibName, EntryPoint = "z_sample_keyexpr", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr z_sample_keyexpr(IntPtr sample);

        // ────────────────────────────────────────────────────────────
        // 5.9 闭包（Closure）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 初始化一个 sample 回调闭包（填充三个函数指针字段）。
        /// <para>C 签名：<c>void z_closure_sample(z_owned_closure_sample_t* closure, void(*call)(const z_loaned_sample_t*, void*), void(*drop)(void*), void* context)</c></para>
        /// <para>
        /// 使用示例（C#）：
        /// <code>
        ///   // 定义回调 delegate（必须用 GCHandle 或静态字段防止 GC）
        ///   [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        ///   delegate void SampleCallDelegate(IntPtr sample, IntPtr ctx);
        ///   static readonly SampleCallDelegate s_onSample = OnSampleReceived;
        ///   static void OnSampleReceived(IntPtr sample, IntPtr ctx) { ... }
        ///
        ///   IntPtr callFp = Marshal.GetFunctionPointerForDelegate(s_onSample);
        ///   ZenohNative.z_closure_sample(out var closure, callFp, IntPtr.Zero, IntPtr.Zero);
        /// </code>
        /// </para>
        /// </summary>
        /// <param name="closure">输出参数，接收填充后的闭包结构体。</param>
        /// <param name="call">消息到达时的回调函数指针，签名 void(*)(const z_loaned_sample_t*, void*)。</param>
        /// <param name="drop">闭包释放时的清理回调，可为 IntPtr.Zero。</param>
        /// <param name="context">传给 call 和 drop 的上下文指针，可为 IntPtr.Zero。</param>
        [DllImport(LibName, EntryPoint = "z_closure_sample", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_closure_sample(
            out z_owned_closure_sample_t  closure,
            IntPtr                        call,
            IntPtr                        drop,
            IntPtr                        context);

        /// <summary>
        /// 释放一个 owned sample 回调闭包。
        /// <para>C 签名：<c>void z_closure_sample_drop(z_moved_closure_sample_t* closure)</c></para>
        /// <para>
        /// 通常无需手动调用：将闭包传给 z_declare_subscriber 后所有权已转移。
        /// 仅在未使用闭包（如声明失败）时才需手动释放。
        /// </para>
        /// </summary>
        /// <param name="closure">要释放的 owned closure（所有权转移）。</param>
        [DllImport(LibName, EntryPoint = "z_closure_sample_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_closure_sample_drop(ref z_owned_closure_sample_t closure);

        // ────────────────────────────────────────────────────────────
        // 5.10 可查询对象（Queryable）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 声明一个可查询对象（响应 Get 请求）。
        /// <para>C 签名：<c>z_result_t z_declare_queryable(const z_loaned_session_t* session, z_owned_queryable_t* queryable, const z_loaned_keyexpr_t* keyexpr, z_moved_closure_query_t* callback, const z_queryable_options_t* options)</c></para>
        /// </summary>
        /// <param name="session">借用的 session 指针（由 z_session_loan 获取）。</param>
        /// <param name="queryable">输出参数，接收声明的 queryable 对象。</param>
        /// <param name="keyexpr">借用的 keyexpr 指针（由 z_view_keyexpr_loan 获取）。</param>
        /// <param name="callback">移动参数，查询回调闭包（所有权转移）。</param>
        /// <param name="options">queryable 选项指针，传 IntPtr.Zero 使用默认值。</param>
        /// <returns>Z_OK (0) 表示成功，负数错误码表示失败。</returns>
        [DllImport(LibName, EntryPoint = "z_declare_queryable", CallingConvention = CallingConvention.Cdecl)]
        public static extern int z_declare_queryable(
            IntPtr                       session,
            out z_owned_queryable_t      queryable,
            IntPtr                       keyexpr,
            ref z_owned_closure_query_t  callback,
            IntPtr                       options);

        /// <summary>
        /// 释放（撤销声明）一个 owned queryable 对象。
        /// <para>C 签名：<c>void z_queryable_drop(z_moved_queryable_t* queryable)</c></para>
        /// </summary>
        /// <param name="queryable">要释放的 owned queryable（所有权转移）。</param>
        [DllImport(LibName, EntryPoint = "z_queryable_drop", CallingConvention = CallingConvention.Cdecl)]
        public static extern void z_queryable_drop(ref z_owned_queryable_t queryable);

        // ────────────────────────────────────────────────────────────
        // 5.11 实用工具（Utility）
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 探针方法：尝试调用一个轻量级 zenoh-c 函数以检测原生库是否可加载。
        /// <para>
        /// 实现原理：调用 z_config_default()（无网络副作用），紧接 z_config_drop() 释放资源，
        /// 捕获 DllNotFoundException / EntryPointNotFoundException / BadImageFormatException。
        /// </para>
        /// <para>
        /// 典型用法：判断是否在真实模式（有 zenoh-c DLL）下运行，
        /// 若返回 false 则退回模拟模式（参见 RosContext.IsSimulated）。
        /// </para>
        /// </summary>
        /// <returns>true = 原生库成功加载；false = 找不到 DLL 或入口点。</returns>
        public static bool TryLoad()
        {
            try
            {
                // 仅调用轻量级函数（无网络操作、无内存泄漏风险）验证 DLL 是否可加载
                z_config_default(out var cfg);
                z_config_drop(ref cfg);
                return true;
            }
            catch (DllNotFoundException)   { return false; }
            catch (EntryPointNotFoundException) { return false; }
            catch (BadImageFormatException) { return false; }
        }
    }
}
