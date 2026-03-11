using System;
using Ros2.Core;
using Xunit;

namespace ZROS.Tests
{
    public class CoreTests
    {
        [Fact]
        public void RosContext_CanBeCreated()
        {
            using var context = new RosContext();
            Assert.NotNull(context);
        }

        [Fact]
        public void RosContext_IsSimulated_WhenNativeLibraryNotAvailable()
        {
            using var context = new RosContext();
            // In test environment without zenoh-c library, it should be in simulation mode
            Assert.True(context.IsSimulated);
        }

        [Fact]
        public void RosContext_IsOpen_WhenCreated()
        {
            using var context = new RosContext();
            Assert.True(context.IsOpen);
        }

        [Fact]
        public void RosNode_CanBeCreated()
        {
            using var context = new RosContext();
            using var node = context.CreateNode("test_node");
            Assert.NotNull(node);
        }

        [Fact]
        public void RosNode_HasCorrectName()
        {
            using var context = new RosContext();
            using var node = context.CreateNode("my_node");
            Assert.Equal("my_node", node.Name);
        }

        [Fact]
        public void RosNode_HasCorrectDefaultNamespace()
        {
            using var context = new RosContext();
            using var node = context.CreateNode("my_node");
            Assert.Equal("/", node.Namespace);
        }

        [Fact]
        public void RosNode_HasCorrectCustomNamespace()
        {
            using var context = new RosContext();
            var options = new RosNodeOptions { Namespace = "/robot" };
            using var node = context.CreateNode("my_node", options);
            Assert.Equal("/robot", node.Namespace);
        }

        [Fact]
        public void RosNodeOptions_HasCorrectDefaults()
        {
            var options = new RosNodeOptions();
            Assert.Equal("/", options.Namespace);
            Assert.False(options.UseSim);
            Assert.Equal(0, options.DomainId);
        }

        [Fact]
        public void RosContext_Create_Works()
        {
            using var context = RosContext.Create();
            Assert.NotNull(context);
            Assert.True(context.IsOpen);
        }

        [Fact]
        public void RosContext_IsNotOpen_AfterDispose()
        {
            var context = new RosContext();
            context.Dispose();
            Assert.False(context.IsOpen);
        }
    }
}
