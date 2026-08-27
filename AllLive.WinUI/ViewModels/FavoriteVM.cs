using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using AllLive.WinUI.Helper;
using AllLive.WinUI.Models;
using AllLive.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using WinUIUtils = AllLive.WinUI.Helper.Utils;

namespace AllLive.WinUI.ViewModels
{
    public class FavoriteVM : BaseViewModel
    {
        public FavoriteVM()
        {
            Items = new ObservableCollection<FavoriteItem>();
            Groups = new ObservableCollection<FavoriteGroupItem>();
            InputCommand = new RelayCommand(Input);
            OutputCommand = new RelayCommand(Output);
            TipCommand = new RelayCommand(Tip);
            AddGroupCommand = new RelayCommand(AddGroup);
            RenameGroupCommand = new RelayCommand<FavoriteGroupItem>(RenameGroup);
            DeleteGroupCommand = new RelayCommand<FavoriteGroupItem>(DeleteGroup);
            MoveGroupLeftCommand = new RelayCommand<FavoriteGroupItem>(MoveGroupLeft);
            MoveGroupRightCommand = new RelayCommand<FavoriteGroupItem>(MoveGroupRight);
            RefreshCurrentGroupCommand = new RelayCommand(RefreshCurrentGroup);
            MessageCenter.UpdateFavoriteEvent += (s, e) => RefreshFavorite();
        }

        public ICommand InputCommand { get; set; }
        public ICommand OutputCommand { get; set; }
        public ICommand TipCommand { get; set; }
        public ICommand AddGroupCommand { get; set; }
        public ICommand RenameGroupCommand { get; set; }
        public ICommand DeleteGroupCommand { get; set; }
        public ICommand MoveGroupLeftCommand { get; set; }
        public ICommand MoveGroupRightCommand { get; set; }
        public ICommand RefreshCurrentGroupCommand { get; set; }

        /// <summary>
        /// 所属页面 XamlRoot，用于弹窗
        /// </summary>
        public XamlRoot XamlRoot { get; set; }

        /// <summary>
        /// 弹窗可用的 XamlRoot（页面未注入时回退到主窗口）
        /// </summary>
        private XamlRoot DialogXamlRoot
        {
            get { return XamlRoot ?? App.GetMainWindow()?.Content?.XamlRoot; }
        }

        private ObservableCollection<FavoriteItem> _items;
        public ObservableCollection<FavoriteItem> Items
        {
            get { return _items; }
            set { _items = value; DoPropertyChanged("Items"); }
        }

        private ObservableCollection<FavoriteGroupItem> _groups;
        public ObservableCollection<FavoriteGroupItem> Groups
        {
            get { return _groups; }
            set { _groups = value; DoPropertyChanged("Groups"); }
        }

        private FavoriteGroupItem _selectedGroup;
        /// <summary>
        /// 当前选中的分组；null 或 ID 为 0 表示“全部”
        /// </summary>
        public FavoriteGroupItem SelectedGroup
        {
            get { return _selectedGroup; }
            set
            {
                _selectedGroup = value;
                DoPropertyChanged("SelectedGroup");
                DoPropertyChanged("CurrentItems");
            }
        }

        /// <summary>
        /// 当前分组展示的主播集合
        /// </summary>
        public ObservableCollection<FavoriteItem> CurrentItems
        {
            get
            {
                if (SelectedGroup == null || SelectedGroup.ID == 0)
                {
                    return Items;
                }
                return SelectedGroup.Items;
            }
        }


        public override bool Loading
        {
            get { return base.Loading; }
            set
            {
                base.Loading = value;
                DoPropertyChanged("IsRefreshing");
            }
        }

        private bool _loadingLiveStatus;

        public bool LoadingLiveStatus
        {
            get { return _loadingLiveStatus; }
            set
            {
                _loadingLiveStatus = value;
                DoPropertyChanged("LoadingLiveStatus");
                DoPropertyChanged("CanRefresh");
                DoPropertyChanged("IsRefreshing");
            }
        }

        /// <summary>
        /// 是否允许点击刷新（加载直播状态期间禁用，避免并发重复加载）
        /// </summary>
        public bool CanRefresh => !LoadingLiveStatus;

        /// <summary>
        /// 是否正在刷新（加载数据或拉取直播状态）
        /// </summary>
        public bool IsRefreshing => Loading || LoadingLiveStatus;



        public async void LoadData(bool loadLiveStatus = true)
        {

            int maxConcurrencyLevel = SettingHelper.GetValue(SettingHelper.CONCURRENCY_LEVEL, 4);
            var semaphore = new SemaphoreSlim(maxConcurrencyLevel);

            try
            {
                Loading = true;
                Items.Clear();
                Groups.Clear();

                var favorites = await DatabaseHelper.GetFavorites();
                var dbGroups = DatabaseHelper.GetFavoriteGroups();

                // 固定分组：全部、默认分组
                var allGroup = new FavoriteGroupItem() { ID = 0, Name = "全部", IsSpecial = true };
                var defaultGroup = new FavoriteGroupItem() { ID = -1, Name = "默认分组", IsSpecial = true };
                Groups.Add(allGroup);
                Groups.Add(defaultGroup);

                var groupMap = new Dictionary<long, FavoriteGroupItem>();
                foreach (var group in dbGroups)
                {
                    group.IsSpecial = false;
                    groupMap[group.ID] = group;
                    Groups.Add(group);
                }

                foreach (var item in favorites)
                {
                    Items.Add(item);
                    if (item.GroupID == null)
                    {
                        defaultGroup.Items.Add(item);
                    }
                    else if (groupMap.TryGetValue(item.GroupID.Value, out var group))
                    {
                        group.Items.Add(item);
                    }
                    else
                    {
                        // 分组已不存在（异常数据），归入默认分组
                        defaultGroup.Items.Add(item);
                    }
                }

                SelectedGroup = allGroup;
                IsEmpty = Items.Count == 0;
                if (!IsEmpty && loadLiveStatus)
                {
                    LoadLiveStatus(semaphore, null);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            finally
            {
                Loading = false;
            }
        }

        public async void LoadLiveStatus(SemaphoreSlim semaphore, FavoriteGroupItem group = null)
        {
            var list = (group == null || group.ID == 0) ? Items : group.Items;
            if (list.Count == 0)
            {
                return;
            }

            LoadingLiveStatus = true;
            Interlocked.Exchange(ref loadedCount, 0);
            var tasks = new List<Task>();
            foreach (var item in list)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await LoadLiveStatusAsync(item, semaphore, list);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        }

        int loadedCount = 0;
        private async Task LoadLiveStatusAsync(FavoriteItem item, SemaphoreSlim semaphore, ObservableCollection<FavoriteItem> list)
        {
            try
            {
                // 标记开始加载
                await Dispatcher.RunOnUIThreadAsync(
                    () =>
                {
                    item.IsLoadingStatus = true;
                });

                var site = MainVM.Sites.FirstOrDefault(x => x.Name == item.SiteName);
                if (site != null)
                {
                    var status = await site.LiveSite.GetLiveStatus(item.RoomID);
                    var liveRoomDetail = await site.LiveSite.GetRoomDetail(item.RoomID);
                    await Dispatcher.RunOnUIThreadAsync(
                        () =>
                    {
                        item.LiveStatus = status;
                        // 更新头像和用户名（从服务器获取最新信息）
                        if (!string.IsNullOrEmpty(liveRoomDetail.UserAvatar))
                        {
                            item.Photo = liveRoomDetail.UserAvatar;
                        }
                        if (!string.IsNullOrEmpty(liveRoomDetail.UserName))
                        {
                            item.UserName = liveRoomDetail.UserName;
                        }
                        // 持久化到数据库
                        DatabaseHelper.UpdateFavorite(item.ID, item.UserName, item.Photo);
                        item.StatusLoaded = true;
                        item.IsLoadingStatus = false;
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"获取直播状态失败:{item.SiteName}-{item.RoomID}", LogType.ERROR, ex);
                await Dispatcher.RunOnUIThreadAsync(
                    () =>
                {
                    item.StatusLoaded = true;
                    item.IsLoadingStatus = false;
                });
            }
            finally
            {
                var currentCount = Interlocked.Increment(ref loadedCount);
                if (currentCount == list.Count)
                {
                    // 切换到UI线程更新集合
                    await Dispatcher.RunOnUIThreadAsync(
                        () =>
                    {
                        LoadingLiveStatus = false;
                        // 排序：直播 > 回放 > 未直播
                        var sorted = list.OrderByDescending(x => (int)x.LiveStatus).ToList();
                        for (int i = 0; i < sorted.Count; i++)
                        {
                            var oldIndex = list.IndexOf(sorted[i]);
                            if (oldIndex != i)
                            {
                                list.Move(oldIndex, i);
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 重新加载收藏数据但不刷新直播状态
        /// </summary>
        private void RefreshFavorite()
        {
            base.Refresh();
            Items.Clear();
            LoadData(false);
        }

        public override void Refresh()
        {
            base.Refresh();
            Items.Clear();
            LoadData();
        }

        public void RemoveItem(FavoriteItem item)
        {
            try
            {
                DatabaseHelper.DeleteFavorite(item.ID);
                Items.Remove(item);
                // 同时从所在分组集合移除
                foreach (var group in Groups)
                {
                    group.Items.Remove(item);
                }
                IsEmpty = Items.Count == 0;
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

        }

        /// <summary>
        /// 只刷新当前分组内主播的直播状态
        /// </summary>
        public async void RefreshCurrentGroup()
        {
            if (Items.Count == 0)
            {
                return;
            }
            int maxConcurrencyLevel = SettingHelper.GetValue(SettingHelper.CONCURRENCY_LEVEL, 4);
            var semaphore = new SemaphoreSlim(maxConcurrencyLevel);
            LoadLiveStatus(semaphore, SelectedGroup);
        }

        public async void AddGroup()
        {
            var input = new TextBox() { PlaceholderText = "请输入分组名称" };
            var dialog = new ContentDialog
            {
                Title = "新建分组",
                Content = input,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = DialogXamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var name = input.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                WinUIUtils.ShowMessageToast("分组名称不能为空");
                return;
            }
            if (Groups.Any(x => x.Name == name))
            {
                WinUIUtils.ShowMessageToast("分组已存在");
                return;
            }

            var id = DatabaseHelper.AddFavoriteGroup(name);
            if (id < 0)
            {
                return;
            }

            var newGroup = new FavoriteGroupItem()
            {
                ID = id,
                Name = name,
                SortOrder = Groups.Count(x => !x.IsSpecial)
            };
            Groups.Add(newGroup);
            SelectedGroup = newGroup;
        }

        public async void RenameGroup(FavoriteGroupItem group)
        {
            if (group == null || group.IsSpecial)
            {
                return;
            }

            var input = new TextBox() { Text = group.Name };
            var dialog = new ContentDialog
            {
                Title = "重命名分组",
                Content = input,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = DialogXamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var name = input.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                WinUIUtils.ShowMessageToast("分组名称不能为空");
                return;
            }
            if (Groups.Any(x => x.ID != group.ID && x.Name == name))
            {
                WinUIUtils.ShowMessageToast("分组已存在");
                return;
            }

            DatabaseHelper.RenameFavoriteGroup(group.ID, name);
            group.Name = name;
        }

        public async void DeleteGroup(FavoriteGroupItem group)
        {
            if (group == null || group.IsSpecial)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "删除分组",
                Content = $"确定删除分组“{group.Name}”吗？组内主播将移入默认分组。",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = DialogXamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                DatabaseHelper.DeleteFavoriteGroup(group.ID);
                var defaultGroup = Groups.FirstOrDefault(x => x.ID == -1);
                foreach (var item in group.Items.ToList())
                {
                    item.GroupID = null;
                    defaultGroup?.Items.Add(item);
                }
                Groups.Remove(group);
                if (SelectedGroup == group)
                {
                    SelectedGroup = Groups.Count > 0 ? Groups[0] : null;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        public void MoveGroupLeft(FavoriteGroupItem group)
        {
            if (group == null || group.IsSpecial)
            {
                return;
            }
            var index = Groups.IndexOf(group);
            if (index <= 2) // 前两项固定：全部、默认分组
            {
                return;
            }
            Groups.Move(index, index - 1);
            SaveGroupOrder();
        }

        public void MoveGroupRight(FavoriteGroupItem group)
        {
            if (group == null || group.IsSpecial)
            {
                return;
            }
            var index = Groups.IndexOf(group);
            if (index < 0 || index >= Groups.Count - 1)
            {
                return;
            }
            Groups.Move(index, index + 1);
            SaveGroupOrder();
        }

        private void SaveGroupOrder()
        {
            var userGroups = Groups.Where(x => !x.IsSpecial).ToList();
            for (int i = 0; i < userGroups.Count; i++)
            {
                userGroups[i].SortOrder = i;
            }
            try
            {
                DatabaseHelper.UpdateFavoriteGroupOrder(userGroups);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        /// <summary>
        /// 设置关注所属分组（groupId 为 null 表示默认分组）
        /// </summary>
        public void SetItemGroup(FavoriteItem item, long? groupId)
        {
            if (item == null)
            {
                return;
            }
            try
            {
                DatabaseHelper.UpdateFavoriteGroup(item.ID, groupId);
                item.GroupID = groupId;
                // 从所有分组集合中移除
                foreach (var group in Groups)
                {
                    group.Items.Remove(item);
                }
                // 加入目标分组集合
                if (groupId == null)
                {
                    Groups.FirstOrDefault(x => x.ID == -1)?.Items.Add(item);
                }
                else
                {
                    Groups.FirstOrDefault(x => x.ID == groupId)?.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        /// <summary>
        /// 弹窗选择分组并设置（支持新建分组）
        /// </summary>
        public async void SetItemGroupDialog(FavoriteItem item)
        {
            if (item == null)
            {
                return;
            }
            var assignableGroups = Groups.Where(x => !x.IsSpecial).ToList();
            var result = await FavoriteGroupDialog.Show(DialogXamlRoot, assignableGroups, "设置分组", item.GroupID);
            if (!result.IsConfirmed)
            {
                return;
            }

            if (!string.IsNullOrEmpty(result.NewGroupName))
            {
                var id = DatabaseHelper.AddFavoriteGroup(result.NewGroupName);
                if (id < 0)
                {
                    return;
                }
                var newGroup = new FavoriteGroupItem()
                {
                    ID = id,
                    Name = result.NewGroupName,
                    SortOrder = Groups.Count(x => !x.IsSpecial)
                };
                Groups.Add(newGroup);
                result.GroupID = id;
            }
            SetItemGroup(item, result.GroupID);
        }

        public async void Input()
        {

            // 打开文件选择器
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".json");
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.CommitButtonText = "导入";

            var window = App.GetMainWindow();
            if (window != null)
            {
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    var json = await FileIO.ReadTextAsync(file);
                    var items = JsonConvert.DeserializeObject<List<FavoriteJsonItem>>(json);
                    foreach (var item in items)
                    {

                        DatabaseHelper.AddFavorite(new FavoriteItem()
                        {
                            SiteName = item.SiteName,
                            RoomID = item.RoomId,
                            UserName = item.UserName,
                            Photo = item.Face,
                        });
                    }
                    WinUIUtils.ShowMessageToast("导入成功");
                    RefreshFavorite();
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    WinUIUtils.ShowMessageToast("导入失败");
                }
            }
        }

        public async void Output()
        {
            // 打开文件选择器
            FileSavePicker picker = new FileSavePicker();
            picker.FileTypeChoices.Add("Json", new List<string>() { ".json" });
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.SuggestedFileName = "favorite.json";

            var window = App.GetMainWindow();
            if (window != null)
            {
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
            }

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    var items = new List<FavoriteJsonItem>();
                    foreach (var item in Items)
                    {
                        var siteId = "";
                        switch(item.SiteName)
                        {
                            case "哔哩哔哩直播":
                                siteId = "bilibili";
                                break;
                            case "斗鱼直播":
                                siteId = "douyu";
                                break;
                            case "虎牙直播":
                                siteId = "huya";
                                break;
                            case "抖音直播":
                                siteId = "douyin";
                                break;
                        }

                        items.Add(new FavoriteJsonItem()
                        {
                            SiteId = siteId,
                            Id = $"{siteId}_{item.RoomID}",
                            RoomId = item.RoomID,
                            UserName = item.UserName,
                            Face = item.Photo,
                            AddTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.M")
                        });
                    }
                    var json = JsonConvert.SerializeObject(items, Formatting.Indented);
                    await FileIO.WriteTextAsync(file, json);
                    WinUIUtils.ShowMessageToast("导出成功");
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    WinUIUtils.ShowMessageToast("导出失败");
                }
            }


        }

        public async void Tip()
        {
            var dialog = new ContentDialog
            {
                Title = "导入导出说明",
                Content = @"该程序兼容Simple Live，您可以导入Simple Live的关注数据，导出的数据也可以在Simple Live中导入。",
                PrimaryButtonText = "确定",
                XamlRoot = App.GetMainWindow()?.Content?.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
    }

    public class FavoriteJsonItem
    {
        [JsonProperty("siteId")]
        public string SiteId;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("roomId")]
        public string RoomId;

        [JsonProperty("userName")]
        public string UserName;

        [JsonProperty("face")]
        public string Face;

        [JsonProperty("addTime")]
        public string AddTime;

        [JsonIgnore]
        public string SiteName
        {
            get
            {
                switch (SiteId)
                {
                    case "bilibili":
                        return "哔哩哔哩直播";
                    case "douyu":
                        return "斗鱼直播";
                    case "huya":
                        return "虎牙直播";
                    case "douyin":
                        return "抖音直播";
                    default:
                        return "未知";
                }
            }
        }

    }
}
