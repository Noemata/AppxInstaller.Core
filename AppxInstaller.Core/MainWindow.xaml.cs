using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;

using Windows.UI.Popups;
using Windows.Storage.Pickers;

using WinRT.InitializeWithWindow;

namespace AppxInstaller
{
    // inetcpl.cpl

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ProductSetup Setup { get; set; }

        const string ProductName = "SimpleApp";
        //const string ProductVersion = "0.1.0.1"; // Note: Forcing not installed state
        const string ProductVersion = "0.1.0.0"; // Note: Forcing not installed state
        const string HelpMessage = "Install Appx from:";
        const string BundleName = "SimpleApp_1.0.0.0_x64.msixbundle";
        const string CertificateName = "SimpleApp_1.0.0.0_x64.cer";
        const string PackageName = "2495a1ba-4af4-437c-8405-e9cbf8378628_nw7zm7aeazf2e";

        public MainWindow()
        {
            InitializeComponent();
        }

        public void InUiThread(Action action)
        {
            if (this.Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Setup = new ProductSetup(PackageName, ProductName, ProductVersion, BundleName, CertificateName);
            Setup.InUiThread = this.InUiThread;

            DataContext = Setup;
        }

        private void OnDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            WindowState = System.Windows.WindowState.Minimized;
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnFolder(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(Setup.InstallDirectory))
                Process.Start("explorer.exe", Setup.InstallDirectory);
        }

        private void OnInstall(object sender, RoutedEventArgs e)
        {
            Setup.StartInstall();
        }

        private void OnRepare(object sender, RoutedEventArgs e)
        {
            Setup.StartRepair();
        }

        private void OnUninstall(object sender, RoutedEventArgs e)
        {
            Setup.StartUninstall();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Setup.StartCancel();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = Setup.IsRunning;
        }

        private async void OnHelp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string dlgMessage = HelpMessage + (Setup.InstallDirectory != null ? $"\n{Setup.InstallDirectory}" : "");
            var dlg = new MessageDialog(dlgMessage, "Instructions:");
            this.InitializeWinRTChild(dlg);
            await dlg.ShowAsync();
        }
    }
}
