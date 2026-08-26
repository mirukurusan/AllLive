using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AllLive.Core.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Windows.System;

namespace AllLive.WinUI.Helper
{
    /// <summary>
    /// 更新检查
    /// </summary>
    public static class UpdateHelper
    {
        private const string GITHUB_REPO = "mirukurusan/AllLive";
        private const string RELEASE_PAGE_URL = "https://github.com/mirukurusan/AllLive/releases";

        /// <summary>
        /// 检查更新
        /// silent=true 启动时静默检查 无更新时不提示
        /// silent=false 手动检查 无更新或失败时提示
        /// </summary>
        public static async Task CheckForUpdateAsync(XamlRoot xamlRoot, bool silent)
        {
            try
            {
                var release = await GetLatestReleaseAsync();
                if (release == null)
                {
                    if (!silent) Utils.ShowMessageToast("检查更新失败，请稍后重试", xamlRoot: xamlRoot);
                    return;
                }

                var latest = ParseVersion(release.tag_name) ?? ParseVersionFromAssets(release);
                if (latest == null || latest <= Utils.GetAppVersion())
                {
                    if (!silent) Utils.ShowMessageToast("当前已是最新版本", xamlRoot: xamlRoot);
                    return;
                }

                await ShowUpdateDialogAsync(xamlRoot, release, latest);
            }
            catch (Exception ex)
            {
                LogHelper.Log("检查更新失败", LogType.ERROR, ex);
                if (!silent) Utils.ShowMessageToast("检查更新失败，请稍后重试", xamlRoot: xamlRoot);
            }
        }

        /// <summary>
        /// 获取 GitHub 最新 Release 信息
        /// </summary>
        private static async Task<GitHubRelease> GetLatestReleaseAsync()
        {
            var url = $"https://api.github.com/repos/{GITHUB_REPO}/releases/latest";
            var json = await HttpUtil.GetString(url, new Dictionary<string, string>
            {
                { "User-Agent", "AllLive.WinUI" }
            });
            return JsonConvert.DeserializeObject<GitHubRelease>(json);
        }

        /// <summary>
        /// 解析 "v3.0.1"、"3.0.1"、"3.0.1.0" 等格式为四位版本号，便于与当前版本比较
        /// </summary>
        private static Version ParseVersion(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.Trim().TrimStart('v', 'V');
            var parts = text.Split('.');
            int[] numbers = new int[4];
            for (int i = 0; i < numbers.Length && i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out numbers[i])) return null;
            }
            return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        /// <summary>
        /// 标签不规范时，尝试从资产文件名（如 AllLive.WinUI_3.0.1.0_portable_x64.zip）解析版本号
        /// </summary>
        private static Version ParseVersionFromAssets(GitHubRelease release)
        {
            if (release.assets == null) return null;
            foreach (var asset in release.assets)
            {
                if (string.IsNullOrEmpty(asset.name)) continue;
                var start = asset.name.IndexOf("AllLive.WinUI_", StringComparison.OrdinalIgnoreCase);
                if (start < 0) continue;
                var rest = asset.name.Substring(start + "AllLive.WinUI_".Length);
                var end = rest.IndexOf('_');
                if (end < 0) end = rest.Length;
                var version = ParseVersion(rest.Substring(0, end));
                if (version != null) return version;
            }
            return null;
        }

        private static async Task ShowUpdateDialogAsync(XamlRoot xamlRoot, GitHubRelease release, Version latest)
        {
            var url = string.IsNullOrEmpty(release.html_url) ? RELEASE_PAGE_URL : release.html_url;
            var notes = string.IsNullOrWhiteSpace(release.body) ? "请前往发布页查看更新内容。" : release.body;

            var dialog = new ContentDialog
            {
                Title = $"发现新版本 v{latest.ToString(3)}",
                Content = new ScrollViewer
                {
                    MaxHeight = 320,
                    Content = new TextBlock
                    {
                        Text = notes,
                        TextWrapping = TextWrapping.Wrap,
                        IsTextSelectionEnabled = true
                    }
                },
                PrimaryButtonText = "前往发布页",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot ?? App.GetMainWindow()?.Content?.XamlRoot
            };
            dialog.PrimaryButtonClick += async (sender, e) =>
            {
                await Launcher.LaunchUriAsync(new Uri(url));
            };
            await dialog.ShowAsync();
        }
    }

    /// <summary>
    /// GitHub Release 信息（仅解析更新检查需要的字段）
    /// </summary>
    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string tag_name { get; set; }

        [JsonProperty("html_url")]
        public string html_url { get; set; }

        [JsonProperty("body")]
        public string body { get; set; }

        [JsonProperty("assets")]
        public List<GitHubAsset> assets { get; set; }
    }

    public class GitHubAsset
    {
        [JsonProperty("name")]
        public string name { get; set; }
    }
}
