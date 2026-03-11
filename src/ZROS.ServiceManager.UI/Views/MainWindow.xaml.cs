using System.Windows;
using ZROS.ServiceManager.UI.ViewModels;

namespace ZROS.ServiceManager.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            (DataContext as MainWindowViewModel)?.Dispose();
            base.OnClosed(e);
        }
    }
}
