namespace ZROS.ServiceManager.UI.Models
{
    /// <summary>Aggregated system-level statistics displayed on the Dashboard.</summary>
    public class SystemStatusModel : ViewModels.ViewModelBase
    {
        private int _totalServices;
        private int _runningServices;
        private int _stoppedServices;
        private int _faultedServices;
        private double _totalCpuPercent;
        private long _totalMemoryBytes;
        private string _activeRecipeName = "None";

        public int TotalServices    { get => _totalServices;    set => SetProperty(ref _totalServices, value); }
        public int RunningServices  { get => _runningServices;  set => SetProperty(ref _runningServices, value); }
        public int StoppedServices  { get => _stoppedServices;  set => SetProperty(ref _stoppedServices, value); }
        public int FaultedServices  { get => _faultedServices;  set => SetProperty(ref _faultedServices, value); }
        public double TotalCpuPercent  { get => _totalCpuPercent;  set => SetProperty(ref _totalCpuPercent, value); }
        public long TotalMemoryBytes   { get => _totalMemoryBytes;  set => SetProperty(ref _totalMemoryBytes, value); }
        public string ActiveRecipeName { get => _activeRecipeName; set => SetProperty(ref _activeRecipeName, value); }

        public string TotalMemoryText =>
            TotalMemoryBytes > 0 ? $"{TotalMemoryBytes / 1024 / 1024:F0} MB" : "0 MB";

        public string StatusSummary =>
            $"Total: {TotalServices}  |  Running: {RunningServices}  |  Faulted: {FaultedServices}";
    }
}
