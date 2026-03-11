using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.UI.Services
{
    /// <summary>Adapts <see cref="ZROS.ServiceManager.ServiceManager"/> to the UI service interface.</summary>
    public class ServiceManagerService : IServiceManagerService, IDisposable
    {
        private readonly ZROS.ServiceManager.ServiceManager _manager;

        public ServiceManagerService()
        {
            _manager = new ZROS.ServiceManager.ServiceManager();
        }

        public Task StartServiceAsync(string serviceName)   => _manager.StartServiceAsync(serviceName);
        public Task StopServiceAsync(string serviceName)    => _manager.StopServiceAsync(serviceName);
        public Task RestartServiceAsync(string serviceName) => _manager.RestartServiceAsync(serviceName);
        public Task StartAllAsync()                          => _manager.StartAllAsync();
        public Task StopAllAsync()                           => _manager.StopAllAsync();

        public void RegisterService(ServiceDefinition definition) => _manager.RegisterService(definition);
        public void UnregisterService(string serviceName)         => _manager.UnregisterService(serviceName);

        public ServiceStatus GetServiceStatus(string serviceName)           => _manager.GetServiceStatus(serviceName);
        public Dictionary<string, ServiceStatus> GetAllServiceStatus()      => _manager.GetAllServiceStatus();
        public IObservable<ServiceEvent> GetServiceEvents()                  => _manager.GetServiceEvents();

        public void LoadConfiguration(string configPath)             => _manager.LoadConfiguration(configPath);
        public void SaveConfiguration(string configPath)             => _manager.SaveConfiguration(configPath);
        public Recipe LoadRecipe(string recipePath)                   => _manager.LoadRecipe(recipePath);
        public void SaveRecipe(Recipe recipe, string recipePath)      => _manager.SaveRecipe(recipe, recipePath);

        public void Dispose() => _manager.Dispose();
    }
}
