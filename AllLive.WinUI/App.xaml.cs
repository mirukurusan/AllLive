using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using AllLive.Core.Helper;
using AllLive.WinUI.Helper;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NSDanmaku.WinUI.Controls;
using WinUIEx;
using WinRT.Interop;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
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
            Current.UnhandledException += App_UnhandledException;

            // WinUIEx 窗口状态持久化存储：打包/未打包模式统一保存到本地数据目录的 JSON 文件
            WindowManager.PersistenceStorage = new WindowStatePersistence();
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            try
            {
                LogHelper.Log("Unhandled exception in app", LogType.ERROR, e.Exception);
                WinUIUtils.ShowMessageToast("An error occurred, logged");
            }
            catch (Exception) { }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            m_window = new MainWindow();

            // 使用 WinUIEx 记忆并恢复主窗口的位置、尺寸与最大化状态（PersistenceId 需在窗口显示前设置）。
            // 若关闭时仍处于小窗模式，OnMainWindowClosing 会先还原普通窗口尺寸，避免小窗状态被持久化。
            m_window.Closed += OnMainWindowClosing;
            var windowManager = WindowManager.Get(m_window);
            windowManager.PersistenceId = "MainWindow";

            m_window.Activate();

            // 设置窗口任务栏图标
            ApplyAppIcon(m_window);

            // Run async init on a background task to not block the window
            _ = InitializeAsync(e);
        }

        /// <summary>
        /// 为窗口设置应用图标（任务栏）。
        /// 新窗口播放模式下创建的 Window 默认没有图标，任务栏会显示空白文件图标，需要显式设置。
        /// </summary>
        public static void ApplyAppIcon(Window window)
        {
            try
            {
                var appWindow = GetAppWindow(window);
                if (appWindow == null) return;
                string iconPath = GetAppIconPath();
                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
            catch { }
        }

        private static string GetAppIconPath()
        {
            try
            {
                var package = Windows.ApplicationModel.Package.Current;
                if (package != null)
                {
                    return Path.Combine(package.InstalledLocation.Path, "Assets", "App.ico");
                }
            }
            catch { }
            return Path.Combine(AppContext.BaseDirectory, "Assets", "App.ico");
        }

        private async Task InitializeAsync(LaunchActivatedEventArgs e)
        {
            try
            {
                TraceRedirector.EnsureInitialized();
                await DatabaseHelper.InitializeDatabase();

                Danmaku.InitDanmakuDpi();

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
                var window = (Current as App)?.m_window;
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
            appWindow.TitleBar.ButtonForegroundColor = TitleBarButtonColor();
            appWindow.TitleBar.BackgroundColor = Colors.Transparent;
        }

        private static Color TitleBarButtonColor()
        {
            var settingTheme = SettingHelper.GetValue<int>(SettingHelper.THEME, 0);
            if (settingTheme == 1) return Colors.Black;
            if (settingTheme == 2) return Colors.White;
            // 默认跟随系统主题，避免浅色背景下白色按钮不可见
            return new UISettings().GetColorValue(UIColorType.Foreground);
        }

        private static AppWindow GetAppWindow(Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
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
                var hwnd = WindowNative.GetWindowHandle(window);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
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
                DouyinScriptRuntime.Current = new LoggingDouyinScriptRunner(new V8DouyinScriptRunner());
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
                DouyuSignRuntime.Current = new V8DouyuSignRunner();
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
            var hwnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        /// <summary>
        /// 主窗口关闭前：若仍处于小窗模式，先还原进入小窗前的普通窗口尺寸与位置，
        /// 避免 WinUIEx 把小窗模式的状态持久化为普通窗口状态。
        /// 必须在 WindowManager 创建前订阅，以保证先于 WinUIEx 的保存逻辑执行。
        /// </summary>
        private void OnMainWindowClosing(object sender, WindowEventArgs e)
        {
            RestorePreMiniState(GetMainAppWindow());
        }

        // 当前处于小窗模式的窗口 ID 集合，小窗模式的尺寸变化不保存为普通窗口尺寸
        private static readonly HashSet<ulong> _miniAppWindowIds = new HashSet<ulong>();

        // 各窗口进入小窗前的尺寸与位置（按 AppWindow Id）；关闭时若仍处于小窗模式则先还原，避免 WinUIEx 持久化小窗状态
        private static readonly Dictionary<ulong, (Windows.Graphics.SizeInt32 Size, Windows.Graphics.PointInt32 Position)> _preMiniStates =
            new Dictionary<ulong, (Windows.Graphics.SizeInt32 Size, Windows.Graphics.PointInt32 Position)>();

        /// <summary>
        /// 标记窗口是否处于小窗模式（小窗模式的尺寸变化不覆盖普通窗口尺寸）。
        /// </summary>
        public static void SetMiniModeActive(AppWindow appWindow, bool mini)
        {
            if (appWindow == null) return;
            lock (_miniAppWindowIds)
            {
                if (mini) _miniAppWindowIds.Add(appWindow.Id.Value);
                else _miniAppWindowIds.Remove(appWindow.Id.Value);

                if (mini)
                {
                    // 进入小窗前记录普通窗口尺寸与位置；重复调用不覆盖首次记录
                    if (!_preMiniStates.ContainsKey(appWindow.Id.Value))
                    {
                        _preMiniStates[appWindow.Id.Value] = (appWindow.Size, appWindow.Position);
                    }
                }
                else
                {
                    _preMiniStates.Remove(appWindow.Id.Value);
                }
            }
        }

        /// <summary>
        /// 若窗口关闭时仍处于小窗模式，还原进入小窗前的普通窗口尺寸与位置，
        /// 避免 WinUIEx 把小窗状态持久化为普通窗口状态。需在 WindowManager 保存前调用。
        /// </summary>
        public static void RestorePreMiniState(AppWindow appWindow)
        {
            if (appWindow == null) return;
            (Windows.Graphics.SizeInt32 Size, Windows.Graphics.PointInt32 Position)? preMiniState = null;
            lock (_miniAppWindowIds)
            {
                if (_miniAppWindowIds.Contains(appWindow.Id.Value) &&
                    _preMiniStates.TryGetValue(appWindow.Id.Value, out var state))
                {
                    preMiniState = state;
                }
            }
            if (preMiniState == null) return;
            try
            {
                appWindow.Move(preMiniState.Value.Position);
                appWindow.Resize(preMiniState.Value.Size);
            }
            catch { }
        }

        /// <summary>
        /// WinUIEx 窗口状态持久化存储：打包/未打包模式统一保存到本地数据目录的 JSON 文件。
        /// WinUIEx 只向其中写入字符串值（窗口状态的 base64），因此这里仅持久化字符串。
        /// </summary>
        private sealed class WindowStatePersistence : IDictionary<string, object>
        {
            private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
            private readonly string _file;

            public WindowStatePersistence()
            {
                _file = Path.Combine(WinUIUtils.GetLocalFolderPath(), "window-state.json");
                try
                {
                    if (File.Exists(_file))
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(_file));
                        foreach (var property in doc.RootElement.EnumerateObject())
                        {
                            if (property.Value.ValueKind == JsonValueKind.String)
                            {
                                _data[property.Name] = property.Value.GetString();
                            }
                        }
                    }
                }
                catch { }
            }

            private void Save()
            {
                try
                {
                    var json = new JsonObject();
                    foreach (var item in _data)
                    {
                        if (item.Value is string value)
                        {
                            json[item.Key] = value;
                        }
                    }
                    var dir = Path.GetDirectoryName(_file);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(_file, json.ToJsonString());
                }
                catch { }
            }

            public object this[string key]
            {
                get => _data[key];
                set { _data[key] = value; Save(); }
            }

            public ICollection<string> Keys => _data.Keys;

            public ICollection<object> Values => _data.Values;

            public int Count => _data.Count;

            public bool IsReadOnly => false;

            public void Add(string key, object value) { _data.Add(key, value); Save(); }

            public void Add(KeyValuePair<string, object> item) { _data.Add(item.Key, item.Value); Save(); }

            public void Clear() { _data.Clear(); Save(); }

            public bool Contains(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_data).Contains(item);

            public bool ContainsKey(string key) => _data.ContainsKey(key);

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((IDictionary<string, object>)_data).CopyTo(array, arrayIndex);

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();

            public bool Remove(string key)
            {
                bool removed = _data.Remove(key);
                if (removed) Save();
                return removed;
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                bool removed = ((IDictionary<string, object>)_data).Remove(item);
                if (removed) Save();
                return removed;
            }

            public bool TryGetValue(string key, out object value) => _data.TryGetValue(key, out value);
        }
    }
}
