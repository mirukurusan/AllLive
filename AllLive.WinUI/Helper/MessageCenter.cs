using System;
using System.Linq;
using System.Threading.Tasks;
using AllLive.Core.Interface;
using AllLive.Core.Models;
using AllLive.WinUI.Controls;
using AllLive.WinUI.Models;
using AllLive.WinUI.ViewModels;
using AllLive.WinUI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIUtils = AllLive.WinUI.Helper.Utils;

namespace AllLive.WinUI.Helper
{
    public static class MessageCenter
    {
        public delegate void NavigatePageHandler(Type page, object data);
        public static event NavigatePageHandler NavigatePageEvent;
        public delegate void ChangeTitleHandler(string title, string logo);
        public static event ChangeTitleHandler ChangeTitleEvent;
        public static event EventHandler<bool> HideTitlebarEvent;
        public static event EventHandler UpdateFavoriteEvent;
        public static event EventHandler UpdatePanelDisplayModeEvent;
        public async static void OpenLiveRoom(ILiveSite liveSite, LiveRoomItem item)
        {
            var arg = new PageArgs()
            {
                Site = liveSite,
                Data = item
            };

            // 如果是哔哩哔哩
            if (liveSite.Name == "哔哩哔哩直播" && !BiliAccount.Instance.Logined&&!SettingHelper.GetValue(SettingHelper.IGNORE_BILI_LOGIN_TIP,false))
            {
                // 弹窗询问是否登录
                var dialog = new ContentDialog
                {
                    Title = "未登录",
                    Content = "您尚未登录哔哩哔哩账号，部分直播可能无法观看，是否前往登录账号？",
                    PrimaryButtonText = "登录",
                    SecondaryButtonText = "取消",
                    CloseButtonText = "不再提示",
                    XamlRoot = App.GetMainWindow()?.Content?.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var login = await BiliBiliLogin();
                    if (login)
                        NavigatePage(typeof(LiveRoomPage), arg);
                    else
                    {
                        WinUIUtils.ShowMessageToast("未登录成功");
                        NavigatePage(typeof(LiveRoomPage), arg);
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    NavigatePage(typeof(LiveRoomPage), arg);
                }
                else // CloseButton
                {
                    SettingHelper.SetValue(SettingHelper.IGNORE_BILI_LOGIN_TIP, true);
                    NavigatePage(typeof(LiveRoomPage), arg);
                }
                return;
            }
            NavigatePage(typeof(LiveRoomPage), arg);

        }

        public async static void NavigatePage(Type page, object data)
        {
            if(SettingHelper.GetValue(SettingHelper.NEW_WINDOW_LIVEROOM, false)&& page == typeof(LiveRoomPage))
            {
                // WinUI 3 multi-window: create a new Window directly
                var newWindow = new Window();
                var frame = new Frame();
                frame.RequestedTheme = (ElementTheme)SettingHelper.GetValue<int>(SettingHelper.THEME, 0);
                frame.Navigate(typeof(LiveRoomPage), data);
                newWindow.Content = frame;
                newWindow.Activate();

                // 设置任务栏图标，避免新窗口显示为空白文件图标
                App.ApplyAppIcon(newWindow);

                // 记录直播窗口，并隐藏系统默认标题栏（WinUI Desktop），只保留页面内自定义标题栏
                App.SetLiveRoomWindow(newWindow);
                App.ApplyWindowTitleBar(App.GetLiveRoomAppWindow());
                newWindow.Closed += (s, e) =>
                {
                    App.ClearLiveRoomWindow(newWindow);
                    // 窗口关闭时停止直播播放，避免关闭后仍在播放
                    if (frame.Content is LiveRoomPage liveRoomPage)
                    {
                        liveRoomPage.OnWindowClosed();
                    }
                };
            }
            else
            {
                NavigatePageEvent?.Invoke(page, data);
            }
            
        }

        

        public static void ChangeTitle(string title, ILiveSite site = null)
        {
            var logo = "ms-appx:///Assets/Square44x44Logo.png";
            if (site != null)
            {
                var siteInfo = MainVM.Sites.FirstOrDefault(x => x.LiveSite.Equals(site));
                if (siteInfo != null)
                {
                    logo = siteInfo.Logo;
                }
            }

            ChangeTitleEvent?.Invoke(title, logo);
        }
        public static void HideTitlebar(bool show)
        {
            HideTitlebarEvent?.Invoke(null, show);
        }

        public static void UpdateFavorite()
        {
            UpdateFavoriteEvent?.Invoke(null, new EventArgs());
        }
        public static void UpdatePanelDisplayMode()
        {
            UpdatePanelDisplayModeEvent?.Invoke(null, new EventArgs());
        }
        public static async Task<bool> BiliBiliLogin()
        {
            BiliLoginDialog biliLoginDialog = new BiliLoginDialog()
            {
                XamlRoot = App.GetMainWindow()?.Content?.XamlRoot
            };
            await biliLoginDialog.ShowAsync();
            return BiliAccount.Instance.Logined;

        }
    }
    partial class BlankPage : Page { }

}
