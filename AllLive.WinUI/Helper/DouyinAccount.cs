using System;
using System.Linq;
using AllLive.Core;
using AllLive.Core.Helper;
using AllLive.WinUI.ViewModels;

namespace AllLive.WinUI.Helper
{
    public class DouyinAccount
    {
        public event EventHandler OnAccountChanged;

        private static DouyinAccount instance;
        public static DouyinAccount Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DouyinAccount();
                }
                return instance;
            }
        }

        public bool Logined { get; set; } = false;

        public string Cookie
        {
            get
            {
                return SettingHelper.GetValue<string>(SettingHelper.DOUYIN_COOKIE, "");
            }
        }

        public void InitLoginInfo()
        {
            Logined = !string.IsNullOrEmpty(Cookie);
            if (Logined)
            {
                SetDouyinSiteCookie();
            }
        }

        public void SetCookie(string cookie)
        {
            SettingHelper.SetValue(SettingHelper.DOUYIN_COOKIE, cookie);
            Logined = !string.IsNullOrEmpty(cookie);
            SetDouyinSiteCookie();
            OnAccountChanged?.Invoke(this, null);
        }

        public void SetDouyinSiteCookie()
        {
            var site = MainVM.Sites.FirstOrDefault(x => x.SiteType == LiveSite.Douyin);
            if (site != null)
            {
                (site.LiveSite as Douyin).UserCookie = Cookie;
            }
        }

        public void Logout()
        {
            Logined = false;
            SettingHelper.SetValue(SettingHelper.DOUYIN_COOKIE, "");
            SetDouyinSiteCookie();
            OnAccountChanged?.Invoke(this, null);
        }
    }
}
