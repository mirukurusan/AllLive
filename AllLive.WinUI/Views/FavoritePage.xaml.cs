using System;
using System.Linq;
using AllLive.Core.Models;
using AllLive.WinUI.Helper;
using AllLive.WinUI.Models;
using AllLive.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using WinUIUtils = AllLive.WinUI.Helper.Utils;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace AllLive.WinUI.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class FavoritePage : Page
    {
        /// <summary>
        /// 分组拖动使用的自定义数据格式，用于与“拖入主播”的文本格式区分
        /// </summary>
        private const string GroupDragFormat = "AllLive.FavoriteGroup";

        static FavoriteVM _favoriteVM;
        readonly FavoriteVM favoriteVM;

        private ListViewItem _dragHoverContainer;
        private Brush _dragHoverOriginalBrush;
        private FavoriteGroupItem _dragGroupItem;

        public FavoritePage()
        {
            if (_favoriteVM == null)
            {
                _favoriteVM = new FavoriteVM();
                _favoriteVM.Dispatcher = new DispatcherQueueHelper(this.DispatcherQueue);
            }
            favoriteVM = _favoriteVM;
            this.InitializeComponent();
            // Loaded 时页面已挂到可视树，此时 XamlRoot 一定可用
            this.Loaded += (s, e) => favoriteVM.XamlRoot = XamlRoot;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            favoriteVM.XamlRoot = XamlRoot;
            if (favoriteVM.Items.Count == 0)
            {
                favoriteVM.LoadData(SettingHelper.GetValue<bool>(SettingHelper.AUTO_LOAD_LIVE_STATUS, false));
            }

        }

        private void groupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is FavoriteGroupItem group)
            {
                if (favoriteVM.SelectedGroup != group)
                {
                    favoriteVM.SelectedGroup = group;
                }
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

        private void SetGroupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem)?.DataContext as FavoriteItem;
            if (item == null)
            {
                return;
            }
            favoriteVM.SetItemGroupDialog(item);
        }

        private void GroupRenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var group = (sender as MenuFlyoutItem)?.DataContext as FavoriteGroupItem;
            if (group == null)
            {
                return;
            }
            favoriteVM.RenameGroup(group);
        }

        private void GroupDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var group = (sender as MenuFlyoutItem)?.DataContext as FavoriteGroupItem;
            if (group == null)
            {
                return;
            }
            favoriteVM.DeleteGroup(group);
        }

        /// <summary>
        /// 开始拖动收藏项，将收藏 ID 以文本形式放入 DataPackage
        /// </summary>
        private void grid_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items.FirstOrDefault() is FavoriteItem item)
            {
                e.Data.SetText(item.ID.ToString());
                e.Data.RequestedOperation = DataPackageOperation.Move;
            }
        }

        /// <summary>
        /// 开始拖动分组；特殊分组（全部/默认分组）不允许拖动
        /// </summary>
        private void groupList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items.FirstOrDefault() is FavoriteGroupItem group)
            {
                if (group.IsSpecial)
                {
                    e.Cancel = true;
                    return;
                }
                _dragGroupItem = group;
                e.Data.SetData(GroupDragFormat, group.ID.ToString());
                e.Data.RequestedOperation = DataPackageOperation.Move;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void groupList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            _dragGroupItem = null;
            ClearDragHover();
        }

        private void groupList_DragOver(object sender, DragEventArgs e)
        {
            // 分组拖动：仅在非特殊分组上允许落下
            if (e.DataView.Contains(GroupDragFormat))
            {
                var groupTarget = GetGroupAtPoint(e.GetPosition(groupList), out var groupContainer);
                if (groupTarget == null || groupTarget.IsSpecial || groupTarget == _dragGroupItem)
                {
                    ClearDragHover();
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else
                {
                    UpdateDragHover(groupTarget, groupContainer);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                e.Handled = true;
                return;
            }

            if (!e.DataView.Contains(StandardDataFormats.Text))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }

            var target = GetGroupAtPoint(e.GetPosition(groupList), out var container);
            UpdateDragHover(target, container);
            e.AcceptedOperation = target != null && target.ID != 0
                ? DataPackageOperation.Move
                : DataPackageOperation.None;
            e.Handled = true;
        }

        private void groupList_DragLeave(object sender, DragEventArgs e)
        {
            ClearDragHover();
        }

        private async void groupList_Drop(object sender, DragEventArgs e)
        {
            ClearDragHover();
            if (e.DataView.Contains(GroupDragFormat))
            {
                _dragGroupItem = null;
                var data = await e.DataView.GetDataAsync(GroupDragFormat);
                if (data is string groupIdText && long.TryParse(groupIdText, out var draggedId))
                {
                    var draggedGroup = favoriteVM.Groups.FirstOrDefault(x => x.ID == draggedId);
                    if (draggedGroup != null && !draggedGroup.IsSpecial)
                    {
                        var point = e.GetPosition(groupList);
                        var groupTarget = GetGroupAtPoint(point, out var groupContainer);
                        if (groupTarget != null && !groupTarget.IsSpecial && groupTarget != draggedGroup && groupContainer != null)
                        {
                            // 落在目标分组左半区插入其前，右半区插入其后
                            var topLeft = groupContainer.TransformToVisual(groupList).TransformPoint(new Point(0, 0));
                            var targetIndex = favoriteVM.Groups.IndexOf(groupTarget);
                            if (point.X >= topLeft.X + groupContainer.ActualWidth / 2)
                            {
                                targetIndex++;
                            }
                            favoriteVM.MoveGroup(draggedGroup, targetIndex);
                        }
                    }
                }
                e.Handled = true;
                return;
            }

            if (!e.DataView.Contains(StandardDataFormats.Text))
            {
                return;
            }

            var text = await e.DataView.GetTextAsync();
            if (!int.TryParse(text, out var id))
            {
                return;
            }
            var item = favoriteVM.Items.FirstOrDefault(x => x.ID == id);
            if (item == null)
            {
                return;
            }

            var target = GetGroupAtPoint(e.GetPosition(groupList), out _);
            if (target == null || target.ID == 0)
            {
                // “全部”只是展示视图，不允许投放
                return;
            }

            // “默认分组”在数据库中对应 NULL
            long? targetGroupId = target.ID == -1 ? null : (long?)target.ID;
            if (item.GroupID == targetGroupId)
            {
                return;
            }
            favoriteVM.SetItemGroup(item, targetGroupId);
        }

        /// <summary>
        /// 根据落点坐标查找所在分组及其容器
        /// </summary>
        private FavoriteGroupItem GetGroupAtPoint(Point point, out ListViewItem container)
        {
            container = null;
            foreach (var group in groupList.Items)
            {
                if (groupList.ContainerFromItem(group) is ListViewItem itemContainer)
                {
                    var topLeft = itemContainer.TransformToVisual(groupList).TransformPoint(new Point(0, 0));
                    if (point.X >= topLeft.X && point.X <= topLeft.X + itemContainer.ActualWidth &&
                        point.Y >= topLeft.Y && point.Y <= topLeft.Y + itemContainer.ActualHeight)
                    {
                        container = itemContainer;
                        return group as FavoriteGroupItem;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 高亮当前拖放目标分组
        /// </summary>
        private void UpdateDragHover(FavoriteGroupItem target, ListViewItem container)
        {
            if (target == null || target.ID == 0 || container == null)
            {
                ClearDragHover();
                return;
            }
            if (container == _dragHoverContainer)
            {
                return;
            }
            ClearDragHover();
            _dragHoverContainer = container;
            _dragHoverOriginalBrush = container.Background;
            container.Background = new SolidColorBrush(Color.FromArgb(0x40, 0x00, 0x78, 0xD7));
        }

        private void ClearDragHover()
        {
            if (_dragHoverContainer != null)
            {
                _dragHoverContainer.Background = _dragHoverOriginalBrush;
                _dragHoverContainer = null;
                _dragHoverOriginalBrush = null;
            }
        }
    }
}
