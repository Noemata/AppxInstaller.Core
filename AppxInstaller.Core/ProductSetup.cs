namespace AppxInstaller
{
    public class ProductSetup : AssetSetup
    {
        bool initialCanInstall;
        public bool InitialCanInstall
        {
            get => initialCanInstall;
            set => SetValue(ref initialCanInstall, value);
        }

        bool initialCanRepair;
        public bool InitialCanRepair
        {
            get => initialCanRepair;
            set => SetValue(ref initialCanRepair, value);
        }

        bool initialCanUnInstall;
        public bool InitialCanUnInstall
        {
            get => initialCanUnInstall;
            set => SetValue(ref initialCanUnInstall, value);
        }

        public ProductSetup(string packageName, string productName, string productVersion, string bundleName, string certificateName) : base(packageName, productName, productVersion, bundleName, certificateName)
        {
            InitialCanInstall = true;
            InitialCanRepair = InitialCanUnInstall = true;
        }
    }
}
