using System;
using System.Collections.Generic;
using Xunit;
using ZROS.ServiceManager;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.Tests
{
    public class FaultRecoveryEngineTests
    {
        private static FaultRecoveryEngine CreateEngine(out Dictionary<string, ServiceInstance> instances, out ServiceRegistry registry, out EventBus bus)
        {
            instances = new Dictionary<string, ServiceInstance>(StringComparer.OrdinalIgnoreCase);
            registry = new ServiceRegistry();
            bus = new EventBus();
            return new FaultRecoveryEngine(instances, registry, bus);
        }

        [Fact]
        public void CalculateBackoffDelay_ReturnInitialDelay_ForFirstRetry()
        {
            using var engine = CreateEngine(out _, out _, out _);
            var policy = new RestartPolicy { InitialDelayMs = 1000, BackoffMultiplier = 2.0, MaxDelayMs = 30000 };
            Assert.Equal(1000, engine.CalculateBackoffDelay(0, policy));
        }

        [Fact]
        public void CalculateBackoffDelay_DoublesDelay_OnSecondRetry()
        {
            using var engine = CreateEngine(out _, out _, out _);
            var policy = new RestartPolicy { InitialDelayMs = 1000, BackoffMultiplier = 2.0, MaxDelayMs = 30000 };
            Assert.Equal(2000, engine.CalculateBackoffDelay(1, policy));
        }

        [Fact]
        public void CalculateBackoffDelay_CapsAtMaxDelay()
        {
            using var engine = CreateEngine(out _, out _, out _);
            var policy = new RestartPolicy { InitialDelayMs = 1000, BackoffMultiplier = 2.0, MaxDelayMs = 5000 };
            // 1000 * 2^10 = 1,024,000 -> capped to 5000
            Assert.Equal(5000, engine.CalculateBackoffDelay(10, policy));
        }

        [Fact]
        public void StartStop_DoesNotThrow()
        {
            using var engine = CreateEngine(out _, out _, out _);
            engine.MonitorInterval = TimeSpan.FromMilliseconds(100);
            engine.Start();
            System.Threading.Thread.Sleep(200);
            engine.Stop();
        }

        [Fact]
        public void Start_CalledTwice_DoesNotThrow()
        {
            using var engine = CreateEngine(out _, out _, out _);
            engine.MonitorInterval = TimeSpan.FromSeconds(10);
            engine.Start();
            engine.Start(); // should be idempotent
            engine.Stop();
        }
    }
}
