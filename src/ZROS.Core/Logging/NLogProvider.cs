using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ZROS.Core.Logging
{
    /// <summary>
    /// NLog-based implementation of <see cref="Microsoft.Extensions.Logging.ILoggerProvider"/>.
    /// Wraps NLog to provide structured logging for the ZROS framework.
    /// </summary>
    public sealed class NLogProvider : Microsoft.Extensions.Logging.ILoggerProvider
    {
        private readonly NLogLoggerFactory _factory;
        private bool _disposed;

        public NLogProvider()
        {
            _factory = new NLogLoggerFactory();
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return _factory.CreateLogger(categoryName);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _factory.Dispose();
            }
        }
    }
}
