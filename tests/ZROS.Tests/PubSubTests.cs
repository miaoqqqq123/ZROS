using System;
using ZROS.Core;
using ZROS.Messages.std_msgs;
using Xunit;

namespace ZROS.Tests
{
    public class PubSubTests
    {
        private RosContext CreateSimContext() => new RosContext();

        [Fact]
        public void RosPublisher_CanBeCreated()
        {
            using var context = CreateSimContext();
            using var node = context.CreateNode("pub_node");
            using var publisher = new RosPublisher<StringMsg>(node, "/test_topic");
            Assert.NotNull(publisher);
        }

        [Fact]
        public void RosPublisher_TopicIsCorrect()
        {
            using var context = CreateSimContext();
            using var node = context.CreateNode("pub_node");
            using var publisher = new RosPublisher<StringMsg>(node, "/chatter");
            Assert.Equal("/chatter", publisher.Topic);
        }

        [Fact]
        public void RosPublisher_NodeNameIsCorrect()
        {
            using var context = CreateSimContext();
            using var node = context.CreateNode("my_pub_node");
            using var publisher = new RosPublisher<StringMsg>(node, "/test_topic");
            Assert.Equal("my_pub_node", publisher.NodeName);
        }

        [Fact]
        public void RosPublisher_Publish_IncrementsPublishedCount()
        {
            using var context = CreateSimContext();
            using var node = context.CreateNode("pub_node");
            using var publisher = new RosPublisher<StringMsg>(node, "/test_topic");
            Assert.Equal(0, publisher.PublishedCount);
            publisher.Publish(new StringMsg("hello"));
            Assert.Equal(1, publisher.PublishedCount);
            publisher.Publish(new StringMsg("world"));
            Assert.Equal(2, publisher.PublishedCount);
        }

        [Fact]
        public void RosSubscriber_CanBeCreated()
        {
            using var context = CreateSimContext();
            using var node = context.CreateNode("sub_node");
            using var subscriber = new RosSubscriber<StringMsg>(node, "/test_topic", msg => { });
            Assert.NotNull(subscriber);
        }

        [Fact]
        public void RosSubscriber_TopicIsCorrect()
        {
            using var context = CreateSimContext();
            using var node = context.CreateNode("sub_node");
            using var subscriber = new RosSubscriber<StringMsg>(node, "/chatter", msg => { });
            Assert.Equal("/chatter", subscriber.Topic);
        }

        [Fact]
        public void RosSubscriber_ReceivesMessage_ViaSimulateReceive()
        {
            using var context = CreateSimContext();
            using var node = context.CreateNode("sub_node");

            StringMsg? received = null;
            using var subscriber = new RosSubscriber<StringMsg>(node, "/test_topic", msg =>
            {
                received = msg;
            });

            Assert.Equal(0, subscriber.ReceivedCount);

            var serializer = new JsonMessageSerializer();
            byte[] data = serializer.Serialize(new StringMsg("test data"));
            subscriber.SimulateReceive(data);

            Assert.Equal(1, subscriber.ReceivedCount);
            Assert.NotNull(received);
            Assert.Equal("test data", received!.Data);
        }
    }
}
