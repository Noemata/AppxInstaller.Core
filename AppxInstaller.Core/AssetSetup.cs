using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AppxInstaller
{
    public class AssetSetup : INotifyPropertyChanged
    {
        /// <summary>
        /// The UI thread marshalling delegate. Used to marshall calls to the UI thread
        /// when executed from a non UI thread.
        /// </summary>
        public Action<Action> InUiThread = (action) => action();

        string FullPackageName;
        string BundleName;
        string CertificateName;

        const string installedmessage = "The product is {0}INSTALLED\n\n";

        public AssetSetup(string packageName, string productName, string productVersion, string bundleName, string certificateName)
        {
            ProductName = productName;
            ProductVersion = productVersion;
            BundleName = bundleName;
            CertificateName = certificateName;
            InstallDirectory = AppxBundle.GetAppxFolder();
            IsCurrentlyInstalled = AppxBundle.IsPackageInstalled(packageName, productVersion, out FullPackageName);

            ProductStatus = string.Format(installedmessage, IsCurrentlyInstalled ? "" : "NOT ");
        }

        CancellationTokenSource cancelTransfer = null;

        public async void Install(bool update = false)
        {
            Progress<AppxProgress> progress = new Progress<AppxProgress>(p =>
            {
                if (p.Percentage == 1000)
                {
                    ErrorStatus = p.Result;
                }
                else if (p.Percentage == ProgressTotal)
                {
                    // MP! todo: Resolve what happens on completion.
                    ProgressCurrentPosition = (int)p.Percentage;
                }
                else
                {
                    ProgressCurrentPosition = (int)p.Percentage;
                }
            });

            IsRunning = true;
            ProgressTotal = 100;
            cancelTransfer = new CancellationTokenSource();
            bool result = await AppxBundle.InstallAppx(BundleName, CertificateName, progress, cancelTransfer.Token, update);
            IsRunning = false;
            cancelTransfer = null;
            IsCurrentlyInstalled = result;
            ProductStatus = string.Format(installedmessage, IsCurrentlyInstalled ? "" : "NOT ");
            if (result) CurrentActionName = "Success";
        }

        public async void Uninstall()
        {
            if (!IsCurrentlyInstalled)
                return;

            Progress<AppxProgress> progress = new Progress<AppxProgress>(p =>
            {
                if (p.Percentage == 1000)
                {
                    ErrorStatus = p.Result;
                }
                else if (p.Percentage == ProgressTotal)
                {
                    // MP! todo: Resolve what happens on completion.
                    ProgressCurrentPosition = (int)p.Percentage;
                }
                else
                {
                    ProgressCurrentPosition = (int)p.Percentage;
                }
            });

            IsRunning = true;
            ProgressTotal = 100;
            cancelTransfer = new CancellationTokenSource();
            bool result = await AppxBundle.RemoveAppx(FullPackageName, CertificateName, progress, cancelTransfer.Token);
            IsRunning = false;
            cancelTransfer = null;
            IsCurrentlyInstalled = !result;
            ProductStatus = string.Format(installedmessage, IsCurrentlyInstalled ? "" : "NOT ");
            if (result) CurrentActionName = "Success";
        }

        /// <summary>
        /// Cancel started install.
        /// </summary>
        public virtual void StartCancel()
        {
            if (IsRunning && cancelTransfer != null)
            {
                cancelTransfer.Cancel();
            }
        }

        /// <summary>
        /// Starts the fresh installation.
        /// </summary>
        public virtual void StartInstall()
        {
            if (!IsCurrentlyInstalled)
            {
                Install();
            }
            else
                ErrorStatus = "Product is already installed";
        }

        /// <summary>
        /// Starts the repair installation for the already installed product.
        /// </summary>
        public virtual void StartRepair()
        {
            if (IsCurrentlyInstalled)
            {
                // MP! resolve: Not handling existing installation correctly yet.  Sort out after hanging bug is fixed.
                if (IsCurrentlyInstalled)
                    Install();
                else
                    Install(update: true);
            }
            else
                ErrorStatus = "Product is not installed";
        }

        /// <summary>
        /// Starts the uninstallation of the already installed product.
        /// </summary>
        public virtual void StartUninstall()
        {
            if (IsCurrentlyInstalled)
            {
                Uninstall();
            }
            else
                ErrorStatus = "Product is not installed";
        }

        /// <summary>
        /// Gets or sets the error status.
        /// </summary>
        /// <value>
        /// The error status.
        /// </value>
        string errorStatus;
        public string ErrorStatus
        {
            get => errorStatus;
            set => SetValue(ref errorStatus, value);
        }

        string installDirectory;
        public string InstallDirectory
        {
            get => installDirectory;
            set => SetValue(ref installDirectory, value);
        }

        /// <summary>
        /// Gets or sets the product status (installed or not installed).
        /// </summary>
        /// <value>
        /// The product status.
        /// </value>
        string productStatus;
        public string ProductStatus
        {
            get => productStatus;
            set => SetValue(ref productStatus, value);
        }

        /// <summary>
        /// Gets or sets the product version.
        /// </summary>
        /// <value>
        /// The product version.
        /// </value>
        string productVersion;
        public string ProductVersion
        {
            get => productVersion;
            set => SetValue(ref productVersion, value);
        }

        /// <summary>
        /// Gets or sets the user visible ProductName.
        /// </summary>
        /// <value>
        /// The name of the product.
        /// </value>
        string productName;
        public string ProductName
        {
            get => productName;
            set => SetValue(ref productName, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the setup is in progress.
        /// </summary>
        /// <value>
        /// <c>true</c> if this setup is in progress; otherwise, <c>false</c>.
        /// </value>
        bool isRunning;
        public bool IsRunning
        {
            get => isRunning;
            set
            {
                isRunning = value;

                if (isRunning)
                    NotStarted = false;

                OnPropertyChanged(nameof(IsRunning));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether setup was not started yet. This information
        /// can be useful for implementing "not started" UI state in the setup GUI.  
        /// </summary>
        /// <value>
        ///   <c>true</c> if setup was not started; otherwise, <c>false</c>.
        /// </value>
        bool notStarted = true;
        public bool NotStarted
        {
            get => notStarted;
            set => SetValue(ref notStarted, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the product is currently installed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product is currently installed; otherwise, <c>false</c>.
        /// </value>
        bool isCurrentlyInstalled;
        public bool IsCurrentlyInstalled
        {
            get => isCurrentlyInstalled;
            set
            {
                isCurrentlyInstalled = value;
                SetValue(ref isCurrentlyInstalled, value);

                OnPropertyChanged(nameof(CanInstall));
                OnPropertyChanged(nameof(CanUnInstall));
                OnPropertyChanged(nameof(CanRepair));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the product can be installed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product can install; otherwise, <c>false</c>.
        /// </value>
        public bool CanInstall { get => !IsCurrentlyInstalled; }

        /// <summary>
        /// Gets a value indicating whether the product can be uninstalled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product can uninstall; otherwise, <c>false</c>.
        /// </value>
        public bool CanUnInstall { get => IsCurrentlyInstalled; }

        /// <summary>
        /// Gets a value indicating whether the product can be repaired.
        /// </summary>
        /// <value>
        /// <c>true</c> if the product can repaired; otherwise, <c>false</c>.
        /// </value>
        public bool CanRepair { get => IsCurrentlyInstalled; }

        /// <summary>
        /// Gets or sets the progress total.
        /// </summary>
        /// <value>The progress total.</value>
        int progressTotal = 100;
        public int ProgressTotal
        {
            get => progressTotal;
            protected set
            {
                progressTotal = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the progress current position.
        /// </summary>
        /// <value>The progress current position.</value>
        int progressCurrentPosition = 0;
        public int ProgressCurrentPosition
        {
            get => progressCurrentPosition;
            protected set
            {
                if (ProgressStepDelay > 0)
                    Thread.Sleep(ProgressStepDelay);

                progressCurrentPosition = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The progress step delay. It is a "for-testing" feature. Set it to positive value (number of milliseconds)
        /// to artificially slow down the installation process. The default value is 0.
        /// </summary>
        public static int ProgressStepDelay = 0;

        /// <summary>
        /// Gets or sets the name of the current action.
        /// </summary>
        /// <value>
        /// The name of the current action.
        /// </value>
        string currentActionName = null;
        public string CurrentActionName
        {
            get => currentActionName;
            protected set
            {
                var newValue = (value ?? "").Trim();

                currentActionName = newValue;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the product language.
        /// </summary>
        /// <value>The language.</value>
        int language;
        public int Language
        {
            get => language;
            protected set => SetValue(ref language, value);
        }

        /// <summary>
        /// Gets or sets the product CodePage.
        /// </summary>
        /// <value>The product CodePage.</value>
        int codePage;
        public int CodePage
        {
            get => codePage;
            protected set => SetValue(ref codePage, value);
        }

        /// <summary>
        /// Gets or sets the flag indication the the user can cancel the setup in progress.
        /// </summary>
        /// <value>The can cancel.</value>
        bool canCancel;
        public bool CanCancel
        {
            get => canCancel;
            protected set => SetValue(ref canCancel, value);
        }

        /// <summary>
        /// Gets or sets the setup window caption.
        /// </summary>
        /// <value>The caption.</value>
        string caption;
        public string Caption
        {
            get => caption;
            protected set => SetValue(ref caption, value);
        }

        int ticksPerActionDataMessage;
        protected int TicksPerActionDataMessage
        {
            get => ticksPerActionDataMessage;
            set => SetValue(ref ticksPerActionDataMessage, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the progress steps are changing in the forward direction.
        /// </summary>
        /// <value>
        /// <c>true</c> if the progress changes are in forward direction; otherwise, <c>false</c>.
        /// </value>
        bool isProgressForwardDirection;
        public bool IsProgressForwardDirection
        {
            get => isProgressForwardDirection;
            set => SetValue(ref isProgressForwardDirection, value);
        }

        bool isProgressTimeEstimationAccurate;
        protected bool IsProgressTimeEstimationAccurate
        {
            get => isProgressTimeEstimationAccurate;
            set => SetValue(ref isProgressTimeEstimationAccurate, value);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                InUiThread(() => PropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

        protected void SetValue<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return;
            backingField = value;
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Occurs when some of the current instance property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
