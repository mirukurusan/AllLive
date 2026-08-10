using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using AllLive.Core.Models;
using AllLive.UWP.Helper;
using AllLive.UWP.Models;
using AllLive.UWP.ViewModels;
using AllLive.UWP.Views;
using LiveSite = AllLive.Core.Helper.LiveSite;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using SiteParser = AllLive.Core.Helper.SiteParser;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace AllLive.UWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {

            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            this.InitializeComponent();
            MessageCenter.UpdatePanelDisplayModeEvent += MessageCenter_UpdatePanelDisplayModeEvent;
            this.KeyDown += MainPage_KeyDown;
            SetPaneMode();
        }

        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.GamepadMenu)
            {
                e.Handled = true;
                // 切换设置

                navigationView.SelectedItem = navigationView.SettingsItem;
            }
            else if (e.Key == VirtualKey.GamepadY)
            {
                e.Handled = true;
                searchBox.Focus(FocusState.Programmatic);
            }
        }

        private void MessageCenter_UpdatePanelDisplayModeEvent(object sender, EventArgs e)
        {
            SetPaneMode();
        }

        private void SetPaneMode()
        {
            if (Utils.IsXbox)
            {
                navigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                MessageCenter.HideTitlebar(true);
                return;
            }
            if (SettingHelper.GetValue<int>(SettingHelper.PANE_DISPLAY_MODE, 0) == 0)
            {
                navigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            }
            else
            {
                navigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _ = BiliAccount.Instance.InitLoginInfo();
            DouyinAccount.Instance.InitLoginInfo();
            _ = CheckUpdate();
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var item = args.SelectedItem as NavigationViewItem;
            if (item.Tag.ToString() == "设置" || item.Tag.ToString() == "Settings")
            {
                item.Tag = "SettingsPage";
            }
            frame.Navigate(Type.GetType("AllLive.UWP.Views." + item.Tag));

        }

        private async void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.QueryText))
            {
                Utils.ShowMessageToast("关键字不能为空");
                return;
            }
            if (!await ParseUrl(args.QueryText))
            {
                this.Frame.Navigate(typeof(SearchPage), args.QueryText);
            }
        }

        private async Task<bool> ParseUrl(string url)
        {
            var parseResult = await SiteParser.ParseUrl(url);
            if (parseResult.Item1 != LiveSite.Unknown && !string.IsNullOrEmpty(parseResult.Item2))
            {
                this.Frame.Navigate(typeof(LiveRoomPage), new PageArgs()
                {
                    Site = MainVM.Sites[(int)parseResult.Item1].LiveSite,
                    Data = new LiveRoomItem()
                    {
                        RoomID = parseResult.Item2,
                    }
                });
                return true;
            }
            else
            {
                return false;
            }


        }

        private void navigationView_Loaded(object sender, RoutedEventArgs e)
        {
            navigationView.IsPaneOpen = false;
        }

        private async Task CheckUpdate()
        {
            try
            {
                StoreContext context = StoreContext.GetDefault();
                IReadOnlyList<StorePackageUpdate> updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();

                if (updates.Count > 0)
                {
                    MessageDialog dialog = new MessageDialog("发现新版本，是否前往应用商店更新？", "发现新版本");
                    dialog.Commands.Add(new UICommand("确定", async (cmd) =>
                    {
                        var product = await context.GetStoreProductForCurrentAppAsync();
                        // 打开应用商店
                        var uri = new Uri($"ms-windows-store://pdp?productid={product.Product.StoreId}");
                        await Launcher.LaunchUriAsync(uri);
                    }));
                    dialog.Commands.Add(new UICommand("取消"));
                    await dialog.ShowAsync();

                }

            }
            catch (Exception ex)
            {
                LogHelper.Log("CheckUpdate", LogType.ERROR, ex);
                await Utils.CheckVersion();
            }


        }
    }
}
