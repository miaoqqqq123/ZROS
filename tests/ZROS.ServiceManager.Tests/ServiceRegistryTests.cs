using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZROS.ServiceManager;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.Tests
{
    public class ServiceRegistryTests
    {
        private static ServiceDefinition MakeService(string name, params string[] dependsOn) =>
            new ServiceDefinition { Name = name, DependsOn = dependsOn, Executable = "test.exe" };

        [Fact]
        public void RegisterService_AddsServiceToRegistry()
        {
            var registry = new ServiceRegistry();
            registry.RegisterService(MakeService("my_service"));
            Assert.NotNull(registry.GetService("my_service"));
        }

        [Fact]
        public void RegisterService_SetsIdIfEmpty()
        {
            var registry = new ServiceRegistry();
            var def = new ServiceDefinition { Name = "svc", Executable = "test.exe" };
            registry.RegisterService(def);
            Assert.False(string.IsNullOrWhiteSpace(def.Id));
        }

        [Fact]
        public void RegisterService_SetsDisplayNameIfEmpty()
        {
            var registry = new ServiceRegistry();
            var def = new ServiceDefinition { Name = "svc", Executable = "test.exe" };
            registry.RegisterService(def);
            Assert.Equal("svc", def.DisplayName);
        }

        [Fact]
        public void RegisterService_OverwritesExisting()
        {
            var registry = new ServiceRegistry();
            registry.RegisterService(MakeService("svc"));
            var updated = MakeService("svc");
            updated.Description = "updated";
            registry.RegisterService(updated);
            Assert.Equal("updated", registry.GetService("svc")!.Description);
        }

        [Fact]
        public void UnregisterService_RemovesEntry()
        {
            var registry = new ServiceRegistry();
            registry.RegisterService(MakeService("svc"));
            Assert.True(registry.UnregisterService("svc"));
            Assert.Null(registry.GetService("svc"));
        }

        [Fact]
        public void UnregisterService_ReturnsFalseIfNotFound()
        {
            var registry = new ServiceRegistry();
            Assert.False(registry.UnregisterService("nonexistent"));
        }

        [Fact]
        public void Contains_ReturnsTrueForRegisteredService()
        {
            var registry = new ServiceRegistry();
            registry.RegisterService(MakeService("svc"));
            Assert.True(registry.Contains("svc"));
        }

        [Fact]
        public void Contains_ReturnsFalseForUnregisteredService()
        {
            var registry = new ServiceRegistry();
            Assert.False(registry.Contains("nonexistent"));
        }

        [Fact]
        public void GetAllServices_ReturnsAllRegistered()
        {
            var registry = new ServiceRegistry();
            registry.RegisterService(MakeService("a"));
            registry.RegisterService(MakeService("b"));
            registry.RegisterService(MakeService("c"));
            var all = registry.GetAllServices().Select(s => s.Name).OrderBy(n => n).ToList();
            Assert.Equal(new[] { "a", "b", "c" }, all);
        }

        [Fact]
        public void SaveAndLoadJson_RoundTrips()
        {
            var registry = new ServiceRegistry();
            registry.RegisterService(MakeService("a"));
            registry.RegisterService(MakeService("b", "a"));

            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"registry_test_{System.Guid.NewGuid()}.json");
            try
            {
                registry.SaveToFile(path);
                var loaded = new ServiceRegistry();
                loaded.LoadFromFile(path);
                Assert.NotNull(loaded.GetService("a"));
                Assert.NotNull(loaded.GetService("b"));
                Assert.Contains("a", loaded.GetService("b")!.DependsOn);
            }
            finally
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }
    }
}
