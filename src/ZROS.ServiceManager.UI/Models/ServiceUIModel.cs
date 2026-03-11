using System;
using System.Windows.Media;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.UI.Models
{
    /// <summary>UI-layer wrapper around <see cref="ServiceStatus"/> with display helpers.</summary>
    public class ServiceUIModel : ViewModels.ViewModelBase
    {
        private ServiceStatus _status = new ServiceStatus();

        public string Name          => _status.Name;
        public ServiceState State   => _status.State;
        public int ProcessId        => _status.ProcessId;
        public DateTime StartTime   => _status.StartTime;
        public int RestartCount     => _status.RestartCount;
        public long MemoryUsageBytes => _status.MemoryUsageBytes;
        public double CpuUsagePercent => _status.CpuUsagePercent;
        public TimeSpan Uptime      => _status.Uptime;

        public string StateText => _status.State.ToString();

        public Brush StateColor => _status.State switch
        {
            ServiceState.Running  => new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)), // green
            ServiceState.Faulted  => new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)), // red
            ServiceState.Starting => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)), // orange
            ServiceState.Stopping => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)), // orange
            ServiceState.Failed   => new SolidColorBrush(Color.FromRgb(0x8E, 0x44, 0xAD)), // purple
            _                     => new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6))  // gray
        };

        public string MemoryText =>
            MemoryUsageBytes > 0 ? $"{MemoryUsageBytes / 1024 / 1024:F1} MB" : "-";

        public string CpuText =>
            CpuUsagePercent > 0 ? $"{CpuUsagePercent:F1}%" : "-";

        public string UptimeText =>
            Uptime > TimeSpan.Zero
                ? $"{(int)Uptime.TotalHours:D2}:{Uptime.Minutes:D2}:{Uptime.Seconds:D2}"
                : "-";

        public void UpdateFrom(ServiceStatus newStatus)
        {
            _status = newStatus;
            base.OnPropertyChanged(string.Empty); // refresh all bindings
        }
    }
}
