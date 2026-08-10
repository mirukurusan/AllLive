using Windows.ApplicationModel;
using Windows.Storage;
using Microsoft.UI;
using AllLive.Core.Helper;
using AllLive.WinUI.Controls;
using CommunityToolkit.WinUI.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace AllLive.WinUI.Helper
{
    public static class Utils
    {
        private static string _unpackagedDataPath;

        /// <summary>
        /// Get local data folder path — works in both packaged and unpackaged modes.
        /// In unpackaged (portable) mode, data is stored in the application directory;
        /// falls back to LocalAppData when that directory isn't writable (e.g. Program Files).
        /// </summary>
        public static string GetLocalFolderPath()
        {
            try
            {
                return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            }
            catch
            {
                if (_unpackagedDataPath != null) return _unpackagedDataPath;

                var appDir = AppContext.BaseDirectory;
                try
                {
                    Directory.CreateDirectory(appDir);
                    var probe = Path.Combine(appDir, ".alllive_write_test");
                    File.WriteAllText(probe, string.Empty);
                    File.Delete(probe);
                    _unpackagedDataPath = Path.Combine(appDir, "data");
                }
                catch
                {
                    _unpackagedDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AllLive");
                }
                return _unpackagedDataPath;
            }
        }

        /// <summary>
        /// Get the local data folder as a <see cref="StorageFolder"/>, creating it if needed.
        /// </summary>
        public static async Task<StorageFolder> GetLocalFolderAsync()
        {
            var path = GetLocalFolderPath();
            Directory.CreateDirectory(path);
            return await StorageFolder.GetFolderFromPathAsync(path);
        }

        /// <summary>
        /// Get the recording folder (LocalFolder\Recordings), creating it if needed.
        /// Works in both packaged and unpackaged modes.
        /// </summary>
        public static async Task<StorageFolder> GetRecordingFolderAsync()
        {
            var root = await GetLocalFolderAsync();
            return await root.CreateFolderAsync("Recordings", CreationCollisionOption.OpenIfExists);
        }

        /// <summary>
        /// Get app version — works in both packaged and unpackaged modes.
        /// In unpackaged mode <see cref="Windows.ApplicationModel.Package.Current"/>
        /// throws InvalidOperationException (no package identity), so fall back
        /// to the assembly version.
        /// </summary>
        public static Version GetAppVersion()
        {
            try
            {
                var packageVersion = Windows.ApplicationModel.Package.Current.Id.Version;
                return new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            }
            catch
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
            }
        }

        public static bool IsXbox
        {
            get
            {
                return Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox";
            }
        }

        public  static void ShowMessageToast(string message, int seconds = 2)
        {
            MessageToast ms = new MessageToast(message, TimeSpan.FromSeconds(seconds));
            ms.Show();
        }
        public async static Task<bool> ShowDialog(string title, string content)
        {
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消",
                XamlRoot = App.GetMainWindow()?.Content?.XamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary;
        }
        public static bool SetClipboard(string content)
        {
            try
            {
                Windows.ApplicationModel.DataTransfer.DataPackage pack = new Windows.ApplicationModel.DataTransfer.DataPackage();
                pack.SetText(content);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pack);
                Windows.ApplicationModel.DataTransfer.Clipboard.Flush();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public static Task AnimateDoublePropertyAsync(this DependencyObject target, string property, double from, double to, double duration = 250, EasingFunctionBase easingFunction = null)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Storyboard storyboard = AnimateDoubleProperty(target, property, from, to, duration, easingFunction);
            storyboard.Completed += (sender, e) =>
            {
                tcs.SetResult(true);
            };
            return tcs.Task;
        }
        public static Storyboard AnimateDoubleProperty(this DependencyObject target, string property, double from, double to, double duration = 250, EasingFunctionBase easingFunction = null)
        {
            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new SineEase(),
                FillBehavior = FillBehavior.HoldEnd,
                EnableDependentAnimation = true
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);

            storyboard.Children.Add(animation);
            storyboard.FillBehavior = FillBehavior.HoldEnd;
            storyboard.Begin();

            return storyboard;
        }
        public static async Task FadeInAsync(this UIElement element, double duration = 250, EasingFunctionBase easingFunction = null)
        {
            if (element.Opacity < 1.0)
            {
                await AnimateDoublePropertyAsync(element, "Opacity", element.Opacity, 1.0, duration, easingFunction);
            }
        }


        public static async Task FadeOutAsync(this UIElement element, double duration = 250, EasingFunctionBase easingFunction = null)
        {
            if (element.Opacity > 0.0)
            {
                await AnimateDoublePropertyAsync(element, "Opacity", element.Opacity, 0.0, duration, easingFunction);
            }
        }

        public async static Task CheckVersion()
        {
            try
            {
                var url = $"https://cdn.jsdelivr.net/gh/xiaoyaocz/AllLive@master/AllLive.UWP/version.json?ts{new Random().Next(0,99999) }";
                var result = await HttpUtil.GetString(url);
                var ver = JsonConvert.DeserializeObject<NewVersion>(result);
                var appVer = GetAppVersion();
                var num = $"{appVer.Major}{appVer.Minor:00}{appVer.Build:00}";
                var v = int.Parse(num);
                if (ver.versionCode > v)
                {
                    var dialog = new ContentDialog();
                    dialog.Title = $"发现新版本 Ver {ver.version}";
                    TextBlock markdownText = new TextBlock()
                    {
                        Text = ver.message,
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                        IsTextSelectionEnabled = true,
                    };
                    dialog.Content = markdownText;
                    dialog.PrimaryButtonText = "查看详情";
                    dialog.SecondaryButtonText = "忽略";
                    dialog.PrimaryButtonClick += new Windows.Foundation.TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>(async (sender, e) =>
                    {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri(ver.url));
                    });
                    await dialog.ShowAsync();
                }
            }
            catch (Exception)
            {
            }
        }

        public static long GetTimeStamp()
        {
            DateTime dt = DateTime.Now;
            TimeSpan ts = dt - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)ts.TotalMilliseconds;
        }
    }
    public class NewVersion
    {
        public string version { get; set; }
        public int versionCode { get; set; }
        public string message { get; set; }
        public string url { get; set; }
    }
}
