using System.Linq;
using AllLive.Core.Models;
using AllLive.WinUI.Helper;
using AllLive.WinUI.Models;
using AllLive.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinUIUtils = AllLive.WinUI.Helper.Utils;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace AllLive.WinUI.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class FavoritePage : Page
    {
        static FavoriteVM _favoriteVM;
        readonly FavoriteVM favoriteVM;

        public FavoritePage()
        {
            if (_favoriteVM == null)
            {
                _favoriteVM = new FavoriteVM();
                _favoriteVM.Dispatcher = new DispatcherQueueHelper(this.DispatcherQueue);
            }
            favoriteVM = _favoriteVM;
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (favoriteVM.Items.Count == 0)
            {
                favoriteVM.LoadData(SettingHelper.GetValue<bool>(SettingHelper.AUTO_LOAD_LIVE_STATUS, false));
            }

        }

        private void ls_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as FavoriteItem;
            if (item == null)
            {
                return;
            }

            // 调试信息：记录站点名称
            LogHelper.Log($"[FavoritePage] 点击收藏 - SiteName: '{item.SiteName}', RoomID: '{item.RoomID}'", LogType.DEBUG);

            var site = MainVM.Sites.FirstOrDefault(x => x.Name == item.SiteName);
            if (site == null)
            {
                // 站点不存在，可能是收藏数据中的站点已被移除
                LogHelper.Log($"[FavoritePage] 无法找到站点 - SiteName: '{item.SiteName}'", LogType.ERROR);
                LogHelper.Log($"[FavoritePage] 可用站点列表: {string.Join(", ", MainVM.Sites.Select(s => $"'{s.Name}'"))}", LogType.DEBUG);

                // 显示详细的调试信息
                var availableSites = string.Join(", ", MainVM.Sites.Select(s => s.Name));
                WinUIUtils.ShowMessageToast($"无法找到站点\n数据库中: '{item.SiteName}'\n可用站点: {availableSites}", 5000);
                return;
            }

            MessageCenter.OpenLiveRoom(site.LiveSite, new LiveRoomItem()
            {
                RoomID = item.RoomID
            });
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem)?.DataContext as FavoriteItem;
            if (item == null)
            {
                return;
            }

            favoriteVM.RemoveItem(item);
        }
    }
}
