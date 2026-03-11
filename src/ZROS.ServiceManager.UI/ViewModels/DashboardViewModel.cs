using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Threading;
using ZROS.ServiceManager.Models;
using ZROS.ServiceManager.UI.Models;
using ZROS.ServiceManager.UI.Services;

namespace ZROS.ServiceManager.UI.ViewModels
{
    /// <summary>ViewModel for the system dashboard.</summary>
    public class DashboardViewModel : ViewModelBase, IDisposable
    {
        private readonly IServiceManagerService _service;
        private readonly DispatcherTimer _refreshTimer;
        private IDisposable? _eventSubscription;
        private bool _disposed;

        private SystemStatusModel _systemStatus = new SystemStatusModel();

        public SystemStatusModel SystemStatus
        {
            get => _systemStatus;
            private set => SetProperty(ref _systemStatus, value);
        }

        public DashboardViewModel(IServiceManagerService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            _eventSubscription = service.GetServiceEvents()
                .Subscribe(_ => dispatcher.BeginInvoke(RefreshStats));

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _refreshTimer.Tick += (_, _) => RefreshStats();
            _refreshTimer.Start();

            RefreshStats();
        }

        private void RefreshStats()
        {
            var statuses = _service.GetAllServiceStatus();

            _systemStatus.TotalServices   = statuses.Count;
            _systemStatus.RunningServices = statuses.Values.Count(s => s.State == ServiceState.Running);
            _systemStatus.StoppedServices = statuses.Values.Count(s => s.State == ServiceState.Stopped);
            _systemStatus.FaultedServices = statuses.Values.Count(s =>
                s.State == ServiceState.Faulted || s.State == ServiceState.Failed);
            _systemStatus.TotalMemoryBytes  = statuses.Values.Sum(s => s.MemoryUsageBytes);
            _systemStatus.TotalCpuPercent   = statuses.Values.Sum(s => s.CpuUsagePercent);

            OnPropertyChanged(nameof(SystemStatus));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _refreshTimer.Stop();
                _eventSubscription?.Dispose();
            }
        }
    }
}
