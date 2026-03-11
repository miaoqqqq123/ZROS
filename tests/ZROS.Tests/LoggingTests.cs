using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Ros2.Core;
using Ros2.Pub;
using Ros2.Messaging;
using Ros2.Messaging.Messages.std_msgs;
using Ros2.Services;
using Zenoh.Native.Logging;
using Xunit;

namespace ZROS.Tests
{
    /// <summary>
    /// Tests for the NLog-based logging system integration.
    /// </summary>
    public class LoggingTests
    {
        [Fact]
        public void ZrosLoggerFactory_CreateLogger_ReturnsLogger()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            Assert.NotNull(logger);
        }

        [Fact]
        public void ZrosLoggerFactory_CreateLoggerByName_ReturnsLogger()
        {
            var logger = ZrosLoggerFactory.CreateLogger("TestCategory");
            Assert.NotNull(logger);
        }

        [Fact]
        public void ZrosLoggerFactory_GetFactory_ReturnsFactory()
        {
            var factory = ZrosLoggerFactory.GetFactory();
            Assert.NotNull(factory);
        }

        [Fact]
        public void ZrosLoggerFactory_Initialize_IsIdempotent()
        {
            // Calling Initialize multiple times should not throw
            ZrosLoggerFactory.Initialize();
            ZrosLoggerFactory.Initialize();
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            Assert.NotNull(logger);
        }

        [Fact]
        public void LoggerExtensions_InfoDoesNotThrow()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            // Should not throw
            logger.Info("Test info message: {Value}", 42);
        }

        [Fact]
        public void LoggerExtensions_DebugDoesNotThrow()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            logger.Debug("Test debug message");
        }

        [Fact]
        public void LoggerExtensions_WarnDoesNotThrow()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            logger.Warn("Test warning: {Reason}", "unit test");
        }

        [Fact]
        public void LoggerExtensions_ErrorDoesNotThrow()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            logger.Error("Test error message");
        }

        [Fact]
        public void LoggerExtensions_ErrorWithException_DoesNotThrow()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            var ex = new InvalidOperationException("test exception");
            logger.Error(ex, "Error occurred: {Message}", ex.Message);
        }

        [Fact]
        public void LoggerExtensions_FatalDoesNotThrow()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            logger.Fatal("Test fatal message");
        }

        [Fact]
        public void LoggerExtensions_FatalWithException_DoesNotThrow()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            var ex = new Exception("fatal test");
            logger.Fatal(ex, "Fatal error: {Message}", ex.Message);
        }

        [Fact]
        public void LoggerExtensions_TraceDoesNotThrow()
        {
            var logger = ZrosLoggerFactory.CreateLogger<LoggingTests>();
            logger.Trace("Test trace message");
        }

        [Fact]
        public void RosContext_AcceptsInjectedLogger()
        {
            var factory = ZrosLoggerFactory.GetFactory();
            var logger = factory.CreateLogger<RosContext>();
            using var context = new RosContext(logger: logger);
            Assert.NotNull(context);
            Assert.True(context.IsSimulated);
        }

        [Fact]
        public void RosNode_AcceptsInjectedLogger()
        {
            var factory = ZrosLoggerFactory.GetFactory();
            var contextLogger = factory.CreateLogger<RosContext>();
            var nodeLogger = factory.CreateLogger<RosNode>();
            using var context = new RosContext(logger: contextLogger);
            using var node = context.CreateNode("logged_node", logger: nodeLogger);
            Assert.Equal("logged_node", node.Name);
        }

        [Fact]
        public void RosPublisher_AcceptsInjectedLogger()
        {
            var factory = ZrosLoggerFactory.GetFactory();
            using var context = new RosContext();
            using var node = context.CreateNode("pub_node");
            var logger = factory.CreateLogger<RosPublisher<StringMsg>>();
            using var publisher = new RosPublisher<StringMsg>(node, "/test/topic", logger: logger);
            Assert.Equal("/test/topic", publisher.Topic);
        }

        [Fact]
        public void RosSubscriber_AcceptsInjectedLogger()
        {
            var factory = ZrosLoggerFactory.GetFactory();
            using var context = new RosContext();
            using var node = context.CreateNode("sub_node");
            var logger = factory.CreateLogger<RosSubscriber<StringMsg>>();
            using var subscriber = new RosSubscriber<StringMsg>(node, "/test/topic", _ => { }, logger: logger);
            Assert.Equal("/test/topic", subscriber.Topic);
        }

        [Fact]
        public void RosServiceServer_AcceptsInjectedLogger()
        {
            var factory = ZrosLoggerFactory.GetFactory();
            using var context = new RosContext();
            using var node = context.CreateNode("svc_node");
            var logger = factory.CreateLogger<RosServiceServer<StringMsg, StringMsg>>();
            using var server = new RosServiceServer<StringMsg, StringMsg>(
                node, "/test/service", req => new StringMsg { Data = "ok" }, logger: logger);
            Assert.True(server.IsRunning);
        }

        [Fact]
        public void NLogProvider_CanBeCreated()
        {
            using var provider = new NLogProvider();
            Assert.NotNull(provider);
        }

        [Fact]
        public void NLogProvider_CreateLogger_ReturnsLogger()
        {
            using var provider = new NLogProvider();
            var logger = provider.CreateLogger("TestCategory");
            Assert.NotNull(logger);
        }
    }
}
