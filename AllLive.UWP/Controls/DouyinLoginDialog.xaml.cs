using AllLive.UWP.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace AllLive.UWP.Controls
{
    public sealed partial class DouyinLoginDialog : ContentDialog
    {
        private const string CHROME_UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";
        public bool LoginSuccess { get; private set; } = false;

        public DouyinLoginDialog()
        {
            this.InitializeComponent();
            this.Loaded += DouyinLoginDialog_Loaded;
        }

        private void DouyinLoginDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 使用HttpRequestMessage设置Chrome UA，避免EdgeHTML被抖音拦截
            var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.douyin.com"));
            requestMsg.Headers.Add("User-Agent", CHROME_UA);
            webView.NavigateWithHttpRequestMessage(requestMsg);
        }

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
        }

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                txtStatus.Text = "请登录抖音账号，登录成功后点击「完成登录」";
                // 注入UA覆盖脚本
                try
                {
                    _ = sender.InvokeScriptAsync("eval", new[] {
                        $"Object.defineProperty(navigator, 'userAgent', {{get: function(){{ return '{CHROME_UA}'; }}}});"
                    });
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
                var filter = new HttpBaseProtocolFilter();
                var cookieManager = filter.CookieManager;
                var cookies = cookieManager.GetCookies(new Uri("https://www.douyin.com"));

                var cookieParts = new List<string>();
                foreach (var cookie in cookies)
                {
                    cookieParts.Add($"{cookie.Name}={cookie.Value}");
                }

                if (cookieParts.Count == 0)
                {
                    txtStatus.Text = "未检测到Cookie，请先登录";
                    return;
                }

                var cookieStr = string.Join(";", cookieParts);

                // 检查是否包含关键cookie（登录后才有的）
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
                Utils.ShowMessageToast("抖音登录成功");
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
