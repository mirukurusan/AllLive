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

        // 新窗口播放模式下的直播窗口（每次打开直播间会覆盖为最新窗口）
        private static Window m_liveRoomWindow;

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

                ApplyWindowTitleBar(appWindow);
            }
            catch { }
        }

        /// <summary>
        /// 隐藏系统默认标题栏（ExtendsContentIntoTitleBar），并让标题栏按钮透明化。
        /// 新窗口播放模式下用于去掉 "WinUI Desktop" 默认标题栏。
        /// </summary>
        public static void ApplyWindowTitleBar(AppWindow appWindow)
        {
            if (appWindow == null) return;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonForegroundColor = TitltBarButtonColor();
            appWindow.TitleBar.BackgroundColor = Colors.Transparent;
        }

        private static Color TitltBarButtonColor()
        {
            var settingTheme = SettingHelper.GetValue<int>(SettingHelper.THEME, 0);
            if (settingTheme == 1) return Colors.Black;
            if (settingTheme == 2) return Colors.White;
            // 默认跟随系统主题，避免浅色背景下白色按钮不可见
            return new UISettings().GetColorValue(UIColorType.Foreground);
        }

        private static AppWindow GetAppWindow(Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        /// <summary>
        /// 根据页面 XamlRoot 获取所在窗口的 AppWindow，用于多窗口场景定位当前窗口。
        /// </summary>
        public static AppWindow GetAppWindow(XamlRoot xamlRoot)
        {
            try
            {
                var env = xamlRoot?.ContentIslandEnvironment;
                if (env != null)
                {
                    return AppWindow.GetFromWindowId(env.AppWindowId);
                }
            }
            catch { }
            return null;
        }

        // 新窗口播放模式下，记录当前直播窗口，便于 LiveRoomVM 等设置窗口标题
        public static void SetLiveRoomWindow(Window window)
        {
            m_liveRoomWindow = window;
        }

        public static void ClearLiveRoomWindow(Window window)
        {
            if (ReferenceEquals(m_liveRoomWindow, window))
            {
                m_liveRoomWindow = null;
            }
        }

        public static AppWindow GetLiveRoomAppWindow()
        {
            var window = m_liveRoomWindow;
            if (window == null) return null;
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                return AppWindow.GetFromWindowId(windowId);
            }
            catch { return null; }
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
