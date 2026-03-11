using System;
using Ros2.Messaging;
using Ros2.Messaging.Messages.std_msgs;
using Ros2.Messaging.Messages.geometry_msgs;
using Xunit;

namespace ZROS.Tests
{
    public class MessagingTests
    {
        [Fact]
        public void StringMsg_Creation_DefaultData()
        {
            var msg = new StringMsg();
            Assert.Equal("std_msgs/String", msg.MessageType);
            Assert.Equal(string.Empty, msg.Data);
        }

        [Fact]
        public void StringMsg_Creation_WithData()
        {
            var msg = new StringMsg("Hello, ZROS!");
            Assert.Equal("std_msgs/String", msg.MessageType);
            Assert.Equal("Hello, ZROS!", msg.Data);
        }

        [Fact]
        public void Int32Msg_Creation_DefaultData()
        {
            var msg = new Int32Msg();
            Assert.Equal("std_msgs/Int32", msg.MessageType);
            Assert.Equal(0, msg.Data);
        }

        [Fact]
        public void Int32Msg_Creation_WithData()
        {
            var msg = new Int32Msg(42);
            Assert.Equal("std_msgs/Int32", msg.MessageType);
            Assert.Equal(42, msg.Data);
        }

        [Fact]
        public void PoseMsg_Creation_DefaultValues()
        {
            var msg = new PoseMsg();
            Assert.Equal("geometry_msgs/Pose", msg.MessageType);
            Assert.NotNull(msg.Position);
            Assert.NotNull(msg.Orientation);
            Assert.Equal(0.0, msg.Position.X);
            Assert.Equal(0.0, msg.Position.Y);
            Assert.Equal(0.0, msg.Position.Z);
            Assert.Equal(0.0, msg.Orientation.W);
        }

        [Fact]
        public void PoseMsg_Creation_WithValues()
        {
            var position = new PoseMsg.Point(1.0, 2.0, 3.0);
            var orientation = new PoseMsg.Quaternion(0.0, 0.0, 0.0, 1.0);
            var msg = new PoseMsg(position, orientation);
            Assert.Equal(1.0, msg.Position.X);
            Assert.Equal(2.0, msg.Position.Y);
            Assert.Equal(3.0, msg.Position.Z);
            Assert.Equal(1.0, msg.Orientation.W);
        }

        [Fact]
        public void JsonSerializer_StringMsg_RoundTrip()
        {
            var serializer = new JsonMessageSerializer();
            var original = new StringMsg("test message");
            byte[] data = serializer.Serialize(original);
            Assert.NotNull(data);
            Assert.NotEmpty(data);
            var deserialized = serializer.Deserialize<StringMsg>(data);
            Assert.Equal(original.Data, deserialized.Data);
            Assert.Equal(original.MessageType, deserialized.MessageType);
        }

        [Fact]
        public void JsonSerializer_Int32Msg_RoundTrip()
        {
            var serializer = new JsonMessageSerializer();
            var original = new Int32Msg(12345);
            byte[] data = serializer.Serialize(original);
            Assert.NotNull(data);
            Assert.NotEmpty(data);
            var deserialized = serializer.Deserialize<Int32Msg>(data);
            Assert.Equal(original.Data, deserialized.Data);
        }
    }
}
