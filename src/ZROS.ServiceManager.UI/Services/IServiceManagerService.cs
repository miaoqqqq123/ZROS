using System.Collections.Generic;
using System.Threading.Tasks;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.UI.Services
{
    public interface IServiceManagerService
    {
        // Service operations
        Task StartServiceAsync(string serviceName);
        Task StopServiceAsync(string serviceName);
        Task RestartServiceAsync(string serviceName);
        Task StartAllAsync();
        Task StopAllAsync();

        // Registration
        void RegisterService(ServiceDefinition definition);
        void UnregisterService(string serviceName);

        // Status
        ServiceStatus GetServiceStatus(string serviceName);
        Dictionary<string, ServiceStatus> GetAllServiceStatus();
        System.IObservable<ServiceEvent> GetServiceEvents();

        // Configuration
        void LoadConfiguration(string configPath);
        void SaveConfiguration(string configPath);
        Recipe LoadRecipe(string recipePath);
        void SaveRecipe(Recipe recipe, string recipePath);
    }
}
