using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ZROS.ServiceManager.Models;
using ZROS.ServiceManager.UI.Models;
using ZROS.ServiceManager.UI.Services;

namespace ZROS.ServiceManager.UI.ViewModels
{
    /// <summary>ViewModel for the service list view.</summary>
    public class ServiceListViewModel : ViewModelBase, IDisposable
    {
        private readonly IServiceManagerService _service;
        private readonly DispatcherTimer _refreshTimer;
        private IDisposable? _eventSubscription;

        private ObservableCollection<ServiceUIModel> _services = new ObservableCollection<ServiceUIModel>();
        private ServiceUIModel? _selectedService;
        private string _filterText = string.Empty;
        private bool _disposed;

        public ObservableCollection<ServiceUIModel> Services
        {
            get => _services;
            private set => SetProperty(ref _services, value);
        }

        public ServiceUIModel? SelectedService
        {
            get => _selectedService;
            set
            {
                SetProperty(ref _selectedService, value);
                ((RelayCommand)StartServiceCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopServiceCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RestartServiceCommand).RaiseCanExecuteChanged();
            }
        }

        public string FilterText
        {
            get => _filterText;
            set { SetProperty(ref _filterText, value); ApplyFilter(); }
        }

        public ICommand StartServiceCommand   { get; }
        public ICommand StopServiceCommand    { get; }
        public ICommand RestartServiceCommand { get; }
        public ICommand RefreshCommand        { get; }
        public ICommand StartAllCommand       { get; }
        public ICommand StopAllCommand        { get; }

        public ServiceListViewModel(IServiceManagerService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            StartServiceCommand   = new RelayCommand(async _ => await ExecuteStartAsync(),   _ => CanStart());
            StopServiceCommand    = new RelayCommand(async _ => await ExecuteStopAsync(),    _ => CanStop());
            RestartServiceCommand = new RelayCommand(async _ => await ExecuteRestartAsync(), _ => CanStop());
            RefreshCommand        = new RelayCommand(_ => Refresh());
            StartAllCommand       = new RelayCommand(async _ => await ExecuteStartAllAsync());
            StopAllCommand        = new RelayCommand(async _ => await ExecuteStopAllAsync());

            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            _eventSubscription = service.GetServiceEvents()
                .Subscribe(evt => dispatcher.BeginInvoke(() => OnServiceEvent(evt)));

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _refreshTimer.Tick += (_, _) => Refresh();
            _refreshTimer.Start();

            Refresh();
        }

        public void Refresh()
        {
            var statuses = _service.GetAllServiceStatus();
            var existing = _services.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in statuses)
            {
                if (existing.TryGetValue(kvp.Key, out var model))
                    model.UpdateFrom(kvp.Value);
                else
                    _services.Add(new ServiceUIModel { }.AlsoUpdate(kvp.Value));
            }

            // Remove services that are no longer registered
            var toRemove = _services.Where(m => !statuses.ContainsKey(m.Name)).ToList();
            foreach (var m in toRemove) _services.Remove(m);
        }

        private void ApplyFilter()
        {
            // Filter is applied in the View via a CollectionView filter predicate
        }

        private void OnServiceEvent(ServiceEvent evt) => Refresh();

        private bool CanStart() => SelectedService != null && SelectedService.State == ServiceState.Stopped;
        private bool CanStop()  => SelectedService != null && SelectedService.State == ServiceState.Running;

        private async Task ExecuteStartAsync()
        {
            if (SelectedService == null) return;
            try { await _service.StartServiceAsync(SelectedService.Name); }
            catch (Exception ex) { ShowError("Start failed", ex); }
        }

        private async Task ExecuteStopAsync()
        {
            if (SelectedService == null) return;
            try { await _service.StopServiceAsync(SelectedService.Name); }
            catch (Exception ex) { ShowError("Stop failed", ex); }
        }

        private async Task ExecuteRestartAsync()
        {
            if (SelectedService == null) return;
            try { await _service.RestartServiceAsync(SelectedService.Name); }
            catch (Exception ex) { ShowError("Restart failed", ex); }
        }

        private async Task ExecuteStartAllAsync()
        {
            try { await _service.StartAllAsync(); }
            catch (Exception ex) { ShowError("Start All failed", ex); }
        }

        private async Task ExecuteStopAllAsync()
        {
            try { await _service.StopAllAsync(); }
            catch (Exception ex) { ShowError("Stop All failed", ex); }
        }

        private void ShowError(string title, Exception ex) =>
            MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);

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

    internal static class ServiceUIModelExtensions
    {
        public static ServiceUIModel AlsoUpdate(this ServiceUIModel model, ServiceStatus status)
        {
            model.UpdateFrom(status);
            return model;
        }
    }
}
