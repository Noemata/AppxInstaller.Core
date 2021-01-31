using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace WinRT.InitializeWithWindow
{
    /// <summary>
    /// <see langword="static"/> <see langword="class"/> that contain the WPF <see cref="Window"/> extension method
    /// </summary>
    public static class WPFWindowExtension
    {
        /// <summary>
        /// COM Import of the native <c>IInitializeWithWindow</c> <see langword="interface"></see> <see href="https://devblogs.microsoft.com/oldnewthing/20190412-00/?p=102413">Related blog post</see> or <seealso href="https://docs.microsoft.com/windows/uwp/design/shell/tiles-and-notifications/secondary-tiles-desktop-pinning">this C# example</seealso>
        /// </summary>
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        public interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }

        /// <summary>
        /// Extension method to initialize WinRT object with this <see cref="Window"/>
        /// </summary>
        /// <param name="window"></param>
        /// <param name="winRTObj"></param>
        public static void InitializeWinRTChild(this Window window, object winRTObj)
        {
            var withWindow = (IInitializeWithWindow)(object)winRTObj;
            withWindow.Initialize(new WindowInteropHelper(window).Handle);
        }

        /// <summary>
        /// Method to initialize WinRT object.
        /// </summary>
        /// <param name="winRTObj"></param>
        /// <param name="window"></param>
        public static void InitializeWinRTChild(object winRTObj, Window window = null)
        {
            var withWindow = (IInitializeWithWindow)(object)winRTObj;
            withWindow.Initialize(new WindowInteropHelper(window == null ? Application.Current.MainWindow : window).Handle);
        }
    }
}
