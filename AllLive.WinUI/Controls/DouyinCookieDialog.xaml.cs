using System;
using AllLive.WinUI.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIUtils = AllLive.WinUI.Helper.Utils;

namespace AllLive.WinUI.Controls
{
    public sealed partial class DouyinCookieDialog : ContentDialog
    {
        public bool LoginSuccess { get; private set; }

        public DouyinCookieDialog()
        {
            this.InitializeComponent();
            this.Loaded += DouyinCookieDialog_Loaded;
        }

        private void DouyinCookieDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 预填当前已保存的Cookie，方便修改
            txtCookie.Text = DouyinAccount.Instance.Cookie;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            var cookie = txtCookie.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(cookie))
            {
                txtStatus.Text = "Cookie 不能为空";
                return;
            }
            if (!cookie.Contains("="))
            {
                txtStatus.Text = "Cookie 格式不正确，需包含类似 ttwid=xxx 的键值对";
                return;
            }
            if (cookie.IndexOf("ttwid", StringComparison.OrdinalIgnoreCase) < 0 &&
                cookie.IndexOf("sessionid", StringComparison.OrdinalIgnoreCase) < 0 &&
                cookie.IndexOf("sid_guard", StringComparison.OrdinalIgnoreCase) < 0)
            {
                txtStatus.Text = "Cookie 中未检测到 ttwid 或登录标识（sessionid/sid_guard），可能无效，请检查后重试";
                return;
            }

            DouyinAccount.Instance.SetCookie(cookie);
            LoginSuccess = true;
            WinUIUtils.ShowMessageToast("抖音Cookie已保存");
            this.Hide();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
