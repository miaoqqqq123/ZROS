using System;
using System.Windows.Input;
using ZROS.ServiceManager.Models;
using ZROS.ServiceManager.UI.Models;
using ZROS.ServiceManager.UI.Services;

namespace ZROS.ServiceManager.UI.ViewModels
{
    /// <summary>ViewModel for the service detail panel.</summary>
    public class ServiceDetailViewModel : ViewModelBase
    {
        private readonly IServiceManagerService _service;
        private ServiceUIModel? _serviceModel;
        private ServiceDefinition? _definition;
        private bool _isEditing;

        public ServiceUIModel? ServiceModel
        {
            get => _serviceModel;
            set { SetProperty(ref _serviceModel, value); LoadDefinition(); }
        }

        public ServiceDefinition? Definition
        {
            get => _definition;
            private set => SetProperty(ref _definition, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool HasSelection => _serviceModel != null;

        public ICommand EditCommand   { get; }
        public ICommand SaveCommand   { get; }
        public ICommand CancelCommand { get; }

        public ServiceDetailViewModel(IServiceManagerService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            EditCommand   = new RelayCommand(_ => { IsEditing = true; },   _ => HasSelection && !IsEditing);
            SaveCommand   = new RelayCommand(_ => SaveDefinition(),         _ => IsEditing);
            CancelCommand = new RelayCommand(_ => { IsEditing = false; LoadDefinition(); }, _ => IsEditing);
        }

        private void LoadDefinition()
        {
            Definition = null;
            IsEditing = false;
            OnPropertyChanged(nameof(HasSelection));
        }

        private void SaveDefinition()
        {
            if (Definition == null) return;
            _service.RegisterService(Definition);
            IsEditing = false;
        }
    }
}
