using AllLive.WinUI.Helper;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.ApplicationModel;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUIUtils = AllLive.WinUI.Helper.Utils;

namespace AllLive.WinUI
{
    public partial class App : Application
    {
        private Window m_window;

        public App()
        {
            this.InitializeComponent();
            App.Current.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            try
            {
                LogHelper.Log("Unhandled exception in app", LogType.ERROR, e.Exception);
                WinUIUtils.ShowMessageToast("An error occurred, logged");
            }
            catch (Exception) { }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            m_window = new MainWindow();
            m_window.Activate();

            // Run async init on a background task to not block the window
            _ = InitializeAsync(e);
        }

        private async System.Threading.Tasks.Task InitializeAsync(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            try
            {
                TraceRedirector.EnsureInitialized();
                await DatabaseHelper.InitializeDatabase();

                NSDanmaku.WinUI.Controls.Danmaku.InitDanmakuDpi();

                m_window.DispatcherQueue.TryEnqueue(() =>
                {
                    var rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    rootFrame.RequestedTheme = (ElementTheme)SettingHelper.GetValue<int>(SettingHelper.THEME, 0);
                    m_window.Content = rootFrame;
                    rootFrame.Navigate(typeof(BaseFramePage), e.Arguments);
                    SetTitleBar();
                });

                InitializeDouyinRuntime();
                InitializeDouyuRuntime();
            }
            catch (Exception ex)
            {
                LogHelper.Log("Init failed", LogType.ERROR, ex);
            }
        }

        public static void SetTitleBar()
        {
            try
            {
                var window = (App.Current as App)?.m_window;
                if (window == null) return;

                var appWindow = GetAppWindow(window);
                if (appWindow == null) return;

                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonForegroundColor = TitltBarButtonColor();
                appWindow.TitleBar.BackgroundColor = Colors.Transparent;
            }
            catch { }
        }

        private static Color TitltBarButtonColor()
        {
            var settingTheme = SettingHelper.GetValue<int>(SettingHelper.THEME, 0);
            if (settingTheme == 1) return Colors.Black;
            if (settingTheme == 2) return Colors.White;
            return Colors.White; // default
        }

        private static AppWindow GetAppWindow(Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void InitializeDouyinRuntime()
        {
            try
            {
                AllLive.Core.Helper.DouyinScriptRuntime.Current = new LoggingDouyinScriptRunner(new V8DouyinScriptRunner());
            }
            catch (Exception ex)
            {
                LogHelper.Log("Failed to initialize DouyinScriptRuntime", LogType.ERROR, ex);
            }
        }

        private void InitializeDouyuRuntime()
        {
            try
            {
                AllLive.Core.Helper.DouyuSignRuntime.Current = new V8DouyuSignRunner();
            }
            catch (Exception ex)
            {
                LogHelper.Log("Failed to initialize DouyuSignRuntime", LogType.ERROR, ex);
            }
        }

        // Called from BaseFramePage to get the main window
        public static Window GetMainWindow()
        {
            return (Current as App)?.m_window;
        }

        // Get AppWindow for the main window (for title bar, fullscreen, etc.)
        public static AppWindow GetMainAppWindow()
        {
            var window = GetMainWindow();
            if (window == null) return null;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}
