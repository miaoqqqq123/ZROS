using System;
using System.Windows;
using System.Windows.Input;
using ZROS.ServiceManager.UI.Services;

namespace ZROS.ServiceManager.UI.ViewModels
{
    /// <summary>ViewModel for the application's main window.</summary>
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly IServiceManagerService _serviceManager;
        private readonly RecipeService _recipeService;
        private readonly ThemeService _themeService;

        private ViewModelBase? _currentView;
        private string _statusBarText = "Ready";
        private bool _disposed;

        public ViewModelBase? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string StatusBarText
        {
            get => _statusBarText;
            set => SetProperty(ref _statusBarText, value);
        }

        public ServiceListViewModel ServiceListVM   { get; }
        public ServiceDetailViewModel ServiceDetailVM { get; }
        public RecipeManagementViewModel RecipeVM   { get; }
        public DashboardViewModel DashboardVM        { get; }

        // Navigation commands
        public ICommand ShowDashboardCommand       { get; }
        public ICommand ShowServiceListCommand     { get; }
        public ICommand ShowRecipeManagementCommand { get; }

        // Service operations
        public ICommand StartAllCommand            { get; }
        public ICommand StopAllCommand             { get; }
        public ICommand RefreshCommand             { get; }

        // Configuration
        public ICommand OpenConfigCommand          { get; }
        public ICommand SaveConfigCommand          { get; }
        public ICommand ToggleThemeCommand         { get; }
        public ICommand ExitCommand                { get; }

        public MainWindowViewModel(IServiceManagerService? serviceManager = null)
        {
            _serviceManager = serviceManager ?? new ServiceManagerService();
            _recipeService  = new RecipeService();
            _themeService   = new ThemeService();

            ServiceListVM   = new ServiceListViewModel(_serviceManager);
            ServiceDetailVM = new ServiceDetailViewModel(_serviceManager);
            RecipeVM        = new RecipeManagementViewModel(_serviceManager, _recipeService);
            DashboardVM     = new DashboardViewModel(_serviceManager);

            // Wire service list selection to detail view
            ServiceListVM.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ServiceListViewModel.SelectedService))
                    ServiceDetailVM.ServiceModel = ServiceListVM.SelectedService;
            };

            ShowDashboardCommand        = new RelayCommand(_ => CurrentView = DashboardVM);
            ShowServiceListCommand      = new RelayCommand(_ => CurrentView = ServiceListVM);
            ShowRecipeManagementCommand = new RelayCommand(_ => CurrentView = RecipeVM);

            StartAllCommand = new RelayCommand(async _ =>
            {
                try { await _serviceManager.StartAllAsync(); SetStatus("All services started."); }
                catch (Exception ex) { SetStatus($"Start All failed: {ex.Message}"); }
            });

            StopAllCommand = new RelayCommand(async _ =>
            {
                try { await _serviceManager.StopAllAsync(); SetStatus("All services stopped."); }
                catch (Exception ex) { SetStatus($"Stop All failed: {ex.Message}"); }
            });

            RefreshCommand = new RelayCommand(_ => ServiceListVM.Refresh());

            OpenConfigCommand = new RelayCommand(_ => ExecuteOpenConfig());
            SaveConfigCommand = new RelayCommand(_ => ExecuteSaveConfig());
            ToggleThemeCommand = new RelayCommand(_ => _themeService.ToggleTheme());
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            // Default view
            CurrentView = DashboardVM;
        }

        private void ExecuteOpenConfig()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Open Configuration",
                Filter = "JSON Files (*.json)|*.json|YAML Files (*.yaml;*.yml)|*.yaml;*.yml|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                _serviceManager.LoadConfiguration(dlg.FileName);
                ServiceListVM.Refresh();
                SetStatus($"Configuration loaded from {dlg.FileName}");
            }
            catch (Exception ex) { SetStatus($"Load failed: {ex.Message}"); }
        }

        private void ExecuteSaveConfig()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title      = "Save Configuration",
                Filter     = "JSON Files (*.json)|*.json",
                DefaultExt = "json"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                _serviceManager.SaveConfiguration(dlg.FileName);
                SetStatus($"Configuration saved to {dlg.FileName}");
            }
            catch (Exception ex) { SetStatus($"Save failed: {ex.Message}"); }
        }

        private void SetStatus(string text) => StatusBarText = text;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                ServiceListVM.Dispose();
                DashboardVM.Dispose();
                (_serviceManager as IDisposable)?.Dispose();
            }
        }
    }
}
