using System.Windows;
using System.Windows.Threading;

namespace blitz
{
    public partial class app : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, a) =>
            {
                try { MessageBox.Show(a.Exception.ToString(), "blitz error", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
                a.Handled = true;
            };
            base.OnStartup(e);
        }
    }
}
