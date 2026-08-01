using AllLive.WinUI.Helper;
using WinUIUtils = AllLive.WinUI.Helper.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AllLive.WinUI.Controls
{
    public sealed partial class DouyinLoginDialog : ContentDialog
    {
        private static readonly string CHROME_UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        public bool LoginSuccess { get; private set; }

        public DouyinLoginDialog()
        {
            this.InitializeComponent();
            this.Loaded += DouyinLoginDialog_Loaded;
        }

        private async void DouyinLoginDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.UserAgent = CHROME_UA;
            webView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
            webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
            webView.CoreWebView2.Navigate("https://www.douyin.com");
            txtStatus.Text = "请登录抖音账号，登录成功后点击「完成登录」";
        }

        private void WebView_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
        }

        private async void WebView_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                // Inject UA override at JS level too
                try
                {
                    await sender.ExecuteScriptAsync(
                        $"Object.defineProperty(navigator, 'userAgent', {{get: function(){{ return '{CHROME_UA}'; }}}});");
                }
                catch { }
            }
            else
            {
                txtStatus.Text = "页面加载失败，请重试";
            }
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            try
            {
                var cookieManager = webView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.douyin.com");

                var cookieParts = cookies
                    .Select(c => $"{c.Name}={c.Value}")
                    .ToList();

                if (cookieParts.Count == 0)
                {
                    txtStatus.Text = "未检测到Cookie，请先登录";
                    return;
                }

                var cookieStr = string.Join(";", cookieParts);

                bool hasSessionId = cookieParts.Any(c =>
                    c.StartsWith("sessionid") ||
                    c.StartsWith("passport_csrf_token") ||
                    c.StartsWith("sid_guard"));

                if (!hasSessionId)
                {
                    txtStatus.Text = "似乎还未登录成功，请确认已登录后再点击完成";
                    return;
                }

                DouyinAccount.Instance.SetCookie(cookieStr);
                LoginSuccess = true;
                WinUIUtils.ShowMessageToast("抖音登录成功");
                this.Hide();
            }
            catch (Exception ex)
            {
                LogHelper.Log("获取抖音Cookie失败", LogType.ERROR, ex);
                txtStatus.Text = "获取Cookie失败: " + ex.Message;
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
