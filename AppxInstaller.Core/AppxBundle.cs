using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using Windows.Management.Deployment;
using Windows.Foundation;

namespace AppxInstaller
{
    public class AppxProgress
    {
        public uint Percentage { get; }
        public string Result { get; }

        public AppxProgress(uint percentage, string result)
        {
            Percentage = percentage;
            Result = result;
        }
    }

    public class AppxBundle
    {
        private static string GetResourcePath(string fileName)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Resources", fileName);
        }

        private static string GetFullResourceName(string shortResourceName)
        {
            return $"{Assembly.GetEntryAssembly().GetName().Name}.Certificate.{shortResourceName}";
        }

        private static void RemoveCertificate(string name)
        {
            using (X509Store store = new X509Store(StoreName.AuthRoot, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite | OpenFlags.IncludeArchived);

                X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindBySubjectName, name, false);

                foreach (var cert in col)
                {
                    // Remove the certificate
                    store.Remove(cert);
                }
            }
        }

        private static void InstallCertificate(string resourceName)
        {
            using (X509Store store = new X509Store(StoreName.AuthRoot, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetFullResourceName(resourceName)))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);

                        byte[] rawData = memoryStream.ToArray();

                        using (X509Certificate2 cert = new X509Certificate2(rawData))
                        {
                            store.Add(cert);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Based on the "Add-AppDevPackage.ps1" that generates when you publish an app with target sideloading.
        /// </summary>
        private static List<Uri> GetDependencies()
        {
            List<Uri> dependencies = new List<Uri>();
            string arch = GetArchitecture();
            string dependenciesPath = GetResourcePath("Dependencies");

            if (string.Equals(arch, "x86") || string.Equals(arch, "amd64") || string.Equals(arch, "arm64"))
            {
                dependencies.AddRange(GetDependencies("x86"));
            }

            if (string.Equals(arch, "amd64"))
            {
                dependencies.AddRange(GetDependencies("x64"));
            }

            if (string.Equals(arch, "arm") || string.Equals(arch, "arm64"))
            {
                dependencies.AddRange(GetDependencies("arm"));
            }

            if (string.Equals(arch, "arm64"))
            {
                dependencies.AddRange(GetDependencies("arm64"));
            }

            return dependencies;

            List<Uri> GetDependencies(string arch)
            {
                string archDepPath = Path.Combine(dependenciesPath, arch);
                if (!Directory.Exists(archDepPath))
                {
                    return new List<Uri>();
                }

                List<Uri> dependencies = new List<Uri>();
                // Dependencies can have the .appx and .msix extension:
                dependencies.AddRange(Directory.GetFiles(archDepPath, @"*.appx", SearchOption.AllDirectories).Select(x => new Uri(x)));
                dependencies.AddRange(Directory.GetFiles(archDepPath, @"*.msix", SearchOption.AllDirectories).Select(x => new Uri(x)));
                return dependencies;
            }

            string GetArchitecture()
            {
                string arch = Environment.GetEnvironmentVariable("Processor_Architecture");

                if (string.Equals(arch, "x86"))
                {
                    if (!(Environment.GetEnvironmentVariable("ProgramFiles(Arm)") is null))
                    {
                        arch = "arm64";
                    }
                    else if (!(Environment.GetEnvironmentVariable("ProgramFiles(x86)") is null))
                    {
                        arch = "amd64";
                    }
                }
                return arch.ToLowerInvariant();
            }
        }

        // MP! todo: add APIs for reinstall and removal of app
        // MP! todo: cleanup UI and code

        public static string GetAppxFolder()
        {
            return GetResourcePath("");
        }

        public static bool IsPackageInstalled(string packageName, string productVersion, out string fullName)
        {
            var pkgManager = new PackageManager();

            var packages = pkgManager.FindPackages();

            fullName = null;

            foreach (var package in packages)
            {
                try
                {
                    var id = package.Id;
                    string name = id.FamilyName;

                    if (name.Equals(packageName))
                    {
                        string version = $"{id.Version.Build}.{id.Version.Major}.{id.Version.Minor}.{id.Version.Revision}";

                        fullName = id.FullName;

                        if (version.Equals(productVersion))
                            return true;
                        else
                        {
                            string[] subparts = productVersion.Split(".");

                            if (subparts.Length != 4)
                                return false;

                            ushort build = 0, major = 0, minor = 0, revision = 0;

                            bool valid = ushort.TryParse(subparts[0], out build) && ushort.TryParse(subparts[1], out major) && ushort.TryParse(subparts[2], out minor) && ushort.TryParse(subparts[3], out revision);

                            if (valid)
                            {
                                if (build > id.Version.Build) return false;
                                if (build < id.Version.Build) return true;
                                if (major > id.Version.Major) return false;
                                if (major < id.Version.Major) return true;
                                if (minor > id.Version.Minor) return false;
                                if (minor < id.Version.Minor) return true;
                                if (revision > id.Version.Revision) return false;
                                if (revision < id.Version.Revision) return true;
                            }

                            return false;
                        }
                    }
                }
                catch { }
            }

            return false;
        }

        public async static Task<bool> InstallAppx(string bundleName, string certificateName, IProgress<AppxProgress> progress, CancellationToken stop = default, bool update = false)
        {
            try
            {
                Uri uriToAppx = new Uri(GetResourcePath(bundleName));
                List<Uri> urisToDependencies = GetDependencies();

                // Cancellation is likely to be impossible for user to trigger for Certificate installation.  Consider adding in slight delay?
                if (stop.IsCancellationRequested)
                    throw new IOException("Install cancelled.");

                InstallCertificate(certificateName);

                bool result = false;

                var pkgManager = new PackageManager();

                IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> operation = null;

                if (update)
                    operation = pkgManager.UpdatePackageAsync(uriToAppx, urisToDependencies, DeploymentOptions.ForceTargetApplicationShutdown);
                else
                    operation = pkgManager.AddPackageAsync(uriToAppx, urisToDependencies, DeploymentOptions.ForceTargetApplicationShutdown);

                operation.Progress = (r, p) =>
                {
                    var appxProgress = new AppxProgress(p.percentage, "...");
                    progress.Report(appxProgress);

                    if (p.percentage == 100)
                        result = r.GetResults().IsRegistered;
                };

                await operation.AsTask(stop);

                return result;
            }
            catch (Exception ex)
            {
                // This exception will also fire when the CancellationToken is cancelled.
                // Set percentage to 1000 to trigger error handling in UI
                var appxProgress = new AppxProgress(1000, ex.Message);
                progress.Report(appxProgress);
                return false;
            }
        }

        public async static Task<bool> RemoveAppx(string fullPackageName, string certificateName, IProgress<AppxProgress> progress, CancellationToken stop = default)
        {
            try
            {
                string shortCertificateName = certificateName.Substring(0, certificateName.IndexOf('_'));

                RemoveCertificate(shortCertificateName);

                bool result = false;

                var pkgManager = new PackageManager();

                IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> operation = pkgManager.RemovePackageAsync(fullPackageName);

                operation.Progress = (r, p) =>
                {
                    var appxProgress = new AppxProgress(p.percentage, "...");
                    progress.Report(appxProgress);

                    if (p.percentage == 100)
                        result = r.GetResults().IsRegistered;
                };

                await operation.AsTask(stop);

                // Give UI a chance to update so user sees 100% done.
                await Task.Delay(150);

                return result;
            }
            catch (Exception ex)
            {
                var appxProgress = new AppxProgress(1000, ex.Message);
                progress.Report(appxProgress);
                return false;
            }
        }
    }
}
