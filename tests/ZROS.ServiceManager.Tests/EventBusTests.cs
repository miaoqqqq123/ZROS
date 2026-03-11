using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using Xunit;
using ZROS.ServiceManager;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.Tests
{
    public class EventBusTests
    {
        [Fact]
        public void Publish_DeliversEventToSubscriber()
        {
            using var bus = new EventBus();
            ServiceEvent? received = null;
            bus.GetEvents().Subscribe(e => received = e);

            var evt = new ServiceEvent("svc", ServiceEventType.Started);
            bus.Publish(evt);

            Assert.NotNull(received);
            Assert.Equal("svc", received!.ServiceName);
            Assert.Equal(ServiceEventType.Started, received.EventType);
        }

        [Fact]
        public void GetEventsFor_FiltersCorrectly()
        {
            using var bus = new EventBus();
            var received = new List<ServiceEvent>();
            bus.GetEventsFor("target").Subscribe(e => received.Add(e));

            bus.Publish(new ServiceEvent("other", ServiceEventType.Started));
            bus.Publish(new ServiceEvent("target", ServiceEventType.Started));

            Assert.Single(received);
            Assert.Equal("target", received[0].ServiceName);
        }

        [Fact]
        public void Publish_AfterDispose_DoesNotThrow()
        {
            var bus = new EventBus();
            bus.Dispose();
            // Publishing after dispose should be silently ignored
            bus.Publish(new ServiceEvent("svc", ServiceEventType.Started));
        }

        [Fact]
        public void ServiceEvent_ToString_ContainsServiceName()
        {
            var evt = new ServiceEvent("my_service", ServiceEventType.Faulted, "test message");
            var str = evt.ToString();
            Assert.Contains("my_service", str);
            Assert.Contains("Faulted", str);
            Assert.Contains("test message", str);
        }

        [Fact]
        public void ServiceEvent_HasTimestamp()
        {
            var before = DateTime.UtcNow;
            var evt = new ServiceEvent("svc", ServiceEventType.Started);
            var after = DateTime.UtcNow;
            Assert.True(evt.Timestamp >= before && evt.Timestamp <= after);
        }
    }
}
