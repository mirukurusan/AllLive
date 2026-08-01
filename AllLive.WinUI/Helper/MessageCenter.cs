using Windows.ApplicationModel;
using Microsoft.UI;
using AllLive.Core.Helper;
using WinUIUtils = AllLive.WinUI.Helper.Utils;
﻿using AllLive.Core.Interface;
using AllLive.Core.Models;
using AllLive.WinUI.Controls;
using AllLive.WinUI.Models;
using AllLive.WinUI.ViewModels;
using AllLive.WinUI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "未登录",
                    Content = "您尚未登录哔哩哔哩账号，部分直播可能无法观看，是否前往登录账号？",
                    PrimaryButtonText = "登录",
                    SecondaryButtonText = "取消",
                    CloseButtonText = "不再提示",
                    XamlRoot = App.GetMainWindow()?.Content?.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
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
                else if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Secondary)
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

            //if (SettingHelper.GetValue(SettingHelper.NEW_WINDOW_LIVEROOM, false))
            //{
            //    CoreApplicationView newView = CoreApplication.CreateNewView();
            //    int newViewId = 0;
            //    await newView.Dispatcher.RunOnUIThreadAsync(() =>
            //    {
            //        Frame frame = new Frame();
            //        frame.Navigate(typeof(LiveRoomPage), arg);
            //        Window.Current.Content = frame;
            //        Window.Current.Activate();
            //        newViewId = ApplicationView.GetForCurrentView().Id;
            //        ApplicationView.GetForCurrentView().Consolidated += (sender, args) =>
            //        {
            //            frame.Navigate(typeof(BlankPage));
            //            CoreWindow.GetForCurrentThread().Close();
            //        };
            //    });
            //    bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
            //}
            //else
            //{
                NavigatePage(typeof(LiveRoomPage), arg);
                //(Window.Current.Content as Frame).Navigate(typeof(LiveRoomPage), arg);
           // }

        }

        public async static void NavigatePage(Type page, object data)
        {
            if(SettingHelper.GetValue(SettingHelper.NEW_WINDOW_LIVEROOM, false)&& page == typeof(LiveRoomPage))
            {
                // WinUI 3 multi-window: create a new Window directly
                var newWindow = new Microsoft.UI.Xaml.Window();
                var frame = new Frame();
                frame.RequestedTheme = (ElementTheme)SettingHelper.GetValue<int>(SettingHelper.THEME, 0);
                frame.Navigate(typeof(LiveRoomPage), data);
                newWindow.Content = frame;
                newWindow.Activate();
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
            BiliLoginDialog biliLoginDialog = new BiliLoginDialog();
            await biliLoginDialog.ShowAsync();
            return BiliAccount.Instance.Logined;

        }
    }
    class BlankPage : Page { }

}
