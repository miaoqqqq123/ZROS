using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using MsILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;

namespace ZROS.Core.Logging
{
    /// <summary>
    /// Central factory for creating <see cref="ILogger"/> instances backed by NLog.
    /// Call <see cref="Initialize"/> once at application startup before creating loggers.
    /// </summary>
    public static class ZrosLoggerFactory
    {
        private static ILoggerFactory? _factory;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the logging system with the specified NLog configuration file.
        /// If <paramref name="configPath"/> is null or the file does not exist, a
        /// built-in default configuration (console + file) is used.
        /// </summary>
        /// <param name="configPath">Path to an nlog.config file, or null for defaults.</param>
        public static void Initialize(string? configPath = null)
        {
            lock (_lock)
            {
                if (_factory != null)
                    return;

                if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
                {
                    LogManager.Configuration = new XmlLoggingConfiguration(configPath);
                }
                else
                {
                    LogManager.Configuration = BuildDefaultConfiguration();
                }

                _factory = new NLogLoggerFactory();
            }
        }

        /// <summary>
        /// Reconfigures the logging system at runtime with a new NLog configuration file.
        /// </summary>
        /// <param name="configPath">Path to an nlog.config file.</param>
        public static void Reconfigure(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                throw new ArgumentException("Config path cannot be null or empty.", nameof(configPath));
            if (!File.Exists(configPath))
                throw new FileNotFoundException("NLog config file not found.", configPath);

            lock (_lock)
            {
                LogManager.Configuration = new XmlLoggingConfiguration(configPath);
                _factory?.Dispose();
                _factory = new NLogLoggerFactory();
            }
        }

        /// <summary>
        /// Sets the global minimum log level at runtime.
        /// </summary>
        public static void SetGlobalLogLevel(NLog.LogLevel level)
        {
            foreach (var rule in LogManager.Configuration?.LoggingRules ?? new List<LoggingRule>())
            {
                rule.SetLoggingLevels(level, LogLevel.Fatal);
            }
            LogManager.ReconfigExistingLoggers();
        }

        /// <summary>
        /// Creates an <see cref="ILogger{T}"/> for the specified type.
        /// Automatically initializes the factory with defaults if not yet initialized.
        /// </summary>
        public static ILogger<T> CreateLogger<T>()
        {
            EnsureInitialized();
            return _factory!.CreateLogger<T>();
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> for the specified category name.
        /// Automatically initializes the factory with defaults if not yet initialized.
        /// </summary>
        public static MsILogger CreateLogger(string categoryName)
        {
            EnsureInitialized();
            return _factory!.CreateLogger(categoryName);
        }

        /// <summary>
        /// Returns the underlying <see cref="ILoggerFactory"/>.
        /// Automatically initializes the factory with defaults if not yet initialized.
        /// </summary>
        public static ILoggerFactory GetFactory()
        {
            EnsureInitialized();
            return _factory!;
        }

        /// <summary>
        /// Shuts down NLog and releases all resources.
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                _factory?.Dispose();
                _factory = null;
                LogManager.Shutdown();
            }
        }

        private static void EnsureInitialized()
        {
            if (_factory == null)
                Initialize();
        }

        private static LoggingConfiguration BuildDefaultConfiguration()
        {
            var config = new LoggingConfiguration();

            var consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${logger:shortName=true} | ${message}${onexception:inner= | ${exception:format=tostring}}"
            };

            var fileTarget = new FileTarget("file")
            {
                FileName = "${basedir}/logs/zros-${shortdate}.log",
                Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${logger} | ${message}${onexception:inner=${newline}${exception:format=tostring}}",
                ArchiveEvery = FileArchivePeriod.Day,
                MaxArchiveFiles = 7,
                KeepFileOpen = false
            };

            var debugTarget = new DebuggerTarget("debugger")
            {
                Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${logger:shortName=true} | ${message}${onexception:inner= | ${exception:format=tostring}}"
            };

            config.AddTarget(consoleTarget);
            config.AddTarget(fileTarget);
            config.AddTarget(debugTarget);

            config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, debugTarget);

            return config;
        }
    }
}
