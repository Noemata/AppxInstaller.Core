# AppxInstaller

Appx installation utility using WPF and .Net Core 3.1.

Description: 

A utility application that installs the content of an embedded Appx and its Certificate file.

## Building
* In MainWindow.xaml.cs adjust string constants PackageName, ProductName, ProductVersion, HelpMessage, BundleName and CertificateName to your requirements.
* Replace the Resources content with your Appx files and folders.
* Replace the Certificate content with your Appx Certificate file (be sure to set the content property to "Embedded resource")
* Publish with "Produce single file" option to create self contained executable with your embedded resource Appx and Certificate.

## Usage
* Select Help ? for usage details.
* Unblock this executable if it is downloaded from a trusted source.

Note: This is a WIP POC that was created to reduce Appx sideloaded installation to one button click.  On first run bundled Appx assets will be extracted to a user account temp folder.  The certificate file remains as an embedded resource within the exectuable binary.  You could opt to encrypt the certificate file.

## Credits
* UI ideas: https://github.com/oleg-shilo/wixsharp
* IInitializeWithWindow lib: https://github.com/mveril
* Package handling ideas: https://github.com/colinkiama/UWP-Package-Installer , https://github.com/UWPX/UWPX-Installer

## Screenshot
![Screenshot](https://github.com/Noemata/AppxInstaller.Core/raw/master/Screenshot.png)

## Publish Settings
![Screenshot](https://github.com/Noemata/AppxInstaller.Core/raw/master/PublishSettings.png)
