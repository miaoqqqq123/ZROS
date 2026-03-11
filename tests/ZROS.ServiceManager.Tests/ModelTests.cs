using System;
using System.IO;
using Xunit;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.Tests
{
    public class ModelTests
    {
        [Fact]
        public void RestartPolicy_DefaultValues_AreCorrect()
        {
            var policy = new RestartPolicy();
            Assert.True(policy.AutoRestart);
            Assert.Equal(3, policy.MaxRetries);
            Assert.Equal(1000, policy.InitialDelayMs);
            Assert.Equal(2.0, policy.BackoffMultiplier);
            Assert.Equal(30000, policy.MaxDelayMs);
            Assert.Equal(60, policy.RestartWindowSeconds);
        }

        [Fact]
        public void HealthCheckConfig_DefaultValues_AreCorrect()
        {
            var config = new HealthCheckConfig();
            Assert.False(config.Enabled);
            Assert.Equal(30, config.IntervalSeconds);
            Assert.Equal(3, config.FailureThreshold);
            Assert.Equal(5, config.TimeoutSeconds);
            Assert.Null(config.HttpEndpoint);
        }

        [Fact]
        public void ServiceDefinition_DefaultValues_AreCorrect()
        {
            var def = new ServiceDefinition();
            Assert.Equal(string.Empty, def.Name);
            Assert.Equal(string.Empty, def.Executable);
            Assert.Empty(def.Arguments);
            Assert.Empty(def.DependsOn);
            Assert.True(def.AutoStart);
            Assert.Equal(ServiceStartType.Automatic, def.StartType);
            Assert.NotNull(def.RestartPolicy);
            Assert.NotNull(def.HealthCheck);
            Assert.NotNull(def.ResourceLimits);
            Assert.NotNull(def.Environment);
        }

        [Fact]
        public void Recipe_DefaultValues_AreCorrect()
        {
            var recipe = new Recipe();
            Assert.Equal(string.Empty, recipe.Id);
            Assert.Equal("1.0.0", recipe.Version);
            Assert.NotNull(recipe.Services);
            Assert.Empty(recipe.Services);
        }

        [Fact]
        public void ServiceStatus_DefaultValues_AreCorrect()
        {
            var status = new ServiceStatus();
            Assert.Equal(string.Empty, status.Name);
            Assert.Equal(ServiceState.Stopped, status.State);
            Assert.Equal(-1, status.ProcessId);
            Assert.Equal(DateTime.MinValue, status.StartTime);
            Assert.Equal(0, status.RestartCount);
            Assert.Equal(TimeSpan.Zero, status.Uptime);
        }

        [Fact]
        public void ServiceEvent_Constructor_SetsProperties()
        {
            var evt = new ServiceEvent("my_svc", ServiceEventType.Started, "hello");
            Assert.Equal("my_svc", evt.ServiceName);
            Assert.Equal(ServiceEventType.Started, evt.EventType);
            Assert.Equal("hello", evt.Message);
        }

        [Fact]
        public void ServiceEvent_Constructor_ThrowsOnNullServiceName()
        {
            Assert.Throws<ArgumentNullException>(() => new ServiceEvent(null!, ServiceEventType.Started));
        }

        [Fact]
        public void ResourceLimits_DefaultValues_AreZero()
        {
            var limits = new ResourceLimits();
            Assert.Equal(0, limits.MaxMemoryMb);
            Assert.Equal(0, limits.MaxCpuPercent);
        }
    }
}
