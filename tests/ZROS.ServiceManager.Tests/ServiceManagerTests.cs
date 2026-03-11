using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZROS.ServiceManager;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.Tests
{
    public class ServiceManagerTests
    {
        private static ServiceDefinition MakeDef(string name, params string[] deps) =>
            new ServiceDefinition
            {
                Name = name,
                DependsOn = deps,
                Executable = "nonexistent.exe",  // will throw on actual start
                AutoStart = true
            };

        [Fact]
        public void RegisterService_AddedToRegistry()
        {
            using var mgr = new ServiceManager();
            mgr.RegisterService(MakeDef("a"));
            var status = mgr.GetServiceStatus("a");
            Assert.Equal("a", status.Name);
        }

        [Fact]
        public void GetServiceStatus_UnknownService_ReturnsStopped()
        {
            using var mgr = new ServiceManager();
            var status = mgr.GetServiceStatus("nonexistent");
            Assert.Equal(ServiceState.Stopped, status.State);
        }

        [Fact]
        public void GetAllServiceStatus_ReturnsAllRegistered()
        {
            using var mgr = new ServiceManager();
            mgr.RegisterService(MakeDef("a"));
            mgr.RegisterService(MakeDef("b"));
            var statuses = mgr.GetAllServiceStatus();
            Assert.True(statuses.ContainsKey("a"));
            Assert.True(statuses.ContainsKey("b"));
        }

        [Fact]
        public async Task StartServiceAsync_NonexistentExecutable_Throws()
        {
            using var mgr = new ServiceManager();
            mgr.RegisterService(MakeDef("svc"));
            await Assert.ThrowsAnyAsync<Exception>(() => mgr.StartServiceAsync("svc"));
        }

        [Fact]
        public async Task StartServiceAsync_UnregisteredService_ThrowsInvalidOperation()
        {
            using var mgr = new ServiceManager();
            await Assert.ThrowsAsync<InvalidOperationException>(() => mgr.StartServiceAsync("nonexistent"));
        }

        [Fact]
        public async Task StopServiceAsync_UnregisteredService_ThrowsInvalidOperation()
        {
            using var mgr = new ServiceManager();
            await Assert.ThrowsAsync<InvalidOperationException>(() => mgr.StopServiceAsync("nonexistent"));
        }

        [Fact]
        public void GetServiceEvents_ReturnsObservable()
        {
            using var mgr = new ServiceManager();
            var events = mgr.GetServiceEvents();
            Assert.NotNull(events);
        }

        [Fact]
        public void RegisterService_PublishesRegisteredEvent()
        {
            using var mgr = new ServiceManager();
            ServiceEvent? received = null;
            mgr.GetServiceEvents().Subscribe(e => received = e);

            mgr.RegisterService(MakeDef("svc"));

            Assert.NotNull(received);
            Assert.Equal(ServiceEventType.Registered, received!.EventType);
        }

        [Fact]
        public void LoadConfiguration_InvalidPath_ThrowsFileNotFound()
        {
            using var mgr = new ServiceManager();
            Assert.Throws<FileNotFoundException>(() => mgr.LoadConfiguration("/nonexistent/path.json"));
        }

        [Fact]
        public void SaveAndLoadRecipe_RoundTrips()
        {
            var path = Path.Combine(Path.GetTempPath(), $"recipe_test_{Guid.NewGuid()}.json");
            try
            {
                var recipe = new Recipe
                {
                    Id = "test_recipe",
                    Name = "Test Recipe",
                    Version = "1.0.0",
                    Services = new List<ServiceDefinition>
                    {
                        MakeDef("svc_a"),
                        MakeDef("svc_b", "svc_a")
                    }
                };

                using var mgr = new ServiceManager();
                mgr.SaveRecipe(recipe, path);

                using var mgr2 = new ServiceManager();
                var loaded = mgr2.LoadRecipe(path);

                Assert.Equal("Test Recipe", loaded.Name);
                Assert.Equal(2, loaded.Services.Count);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public async Task StartAllAsync_CyclicDependency_ThrowsInvalidOperation()
        {
            using var mgr = new ServiceManager();
            mgr.RegisterService(new ServiceDefinition { Name = "a", DependsOn = new[] { "b" }, Executable = "a.exe", AutoStart = true });
            mgr.RegisterService(new ServiceDefinition { Name = "b", DependsOn = new[] { "a" }, Executable = "b.exe", AutoStart = true });
            await Assert.ThrowsAsync<InvalidOperationException>(() => mgr.StartAllAsync());
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var mgr = new ServiceManager();
            mgr.Dispose();
            mgr.Dispose(); // should not throw
        }

        [Fact]
        public void ServiceStatus_Uptime_IsZeroWhenStopped()
        {
            var status = new ServiceStatus { State = ServiceState.Stopped };
            Assert.Equal(TimeSpan.Zero, status.Uptime);
        }

        [Fact]
        public void ServiceStatus_Uptime_IsPositiveWhenRunning()
        {
            var status = new ServiceStatus
            {
                State = ServiceState.Running,
                StartTime = DateTime.UtcNow.AddSeconds(-5)
            };
            Assert.True(status.Uptime.TotalSeconds >= 4);
        }
    }
}
