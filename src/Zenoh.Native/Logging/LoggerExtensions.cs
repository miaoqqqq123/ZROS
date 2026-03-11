using System;
using Microsoft.Extensions.Logging;

namespace Zenoh.Native.Logging
{
    /// <summary>
    /// Extension methods for <see cref="ILogger"/> providing ROS-style convenience logging.
    /// These methods mirror the NLog-style API used in ZRosSystem for easier migration.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>Logs a Trace-level message.</summary>
        public static void Trace(this ILogger logger, string message, params object?[] args)
        {
            logger.Log(LogLevel.Trace, message, args);
        }

        /// <summary>Logs a Debug-level message.</summary>
        public static void Debug(this ILogger logger, string message, params object?[] args)
        {
            logger.Log(LogLevel.Debug, message, args);
        }

        /// <summary>Logs an Information-level message (alias: Info).</summary>
        public static void Info(this ILogger logger, string message, params object?[] args)
        {
            logger.Log(LogLevel.Information, message, args);
        }

        /// <summary>Logs a Warning-level message.</summary>
        public static void Warn(this ILogger logger, string message, params object?[] args)
        {
            logger.Log(LogLevel.Warning, message, args);
        }

        /// <summary>Logs an Error-level message.</summary>
        public static void Error(this ILogger logger, string message, params object?[] args)
        {
            logger.Log(LogLevel.Error, message, args);
        }

        /// <summary>Logs an Error-level message with an associated exception.</summary>
        public static void Error(this ILogger logger, Exception exception, string message, params object?[] args)
        {
            logger.Log(LogLevel.Error, exception, message, args);
        }

        /// <summary>Logs a Critical-level message (alias: Fatal).</summary>
        public static void Fatal(this ILogger logger, string message, params object?[] args)
        {
            logger.Log(LogLevel.Critical, message, args);
        }

        /// <summary>Logs a Critical-level message with an associated exception (alias: Fatal).</summary>
        public static void Fatal(this ILogger logger, Exception exception, string message, params object?[] args)
        {
            logger.Log(LogLevel.Critical, exception, message, args);
        }
    }
}
