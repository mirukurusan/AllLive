using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using AllLive.WinUI.Helper;
using AllLive.WinUI.Models;
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
            InputCommand = new RelayCommand(Input);
            OutputCommand = new RelayCommand(Output);
            TipCommand = new RelayCommand(Tip);
            MessageCenter.UpdateFavoriteEvent += (s, e) => Refresh();
        }

        public ICommand InputCommand { get; set; }
        public ICommand OutputCommand { get; set; }
        public ICommand TipCommand { get; set; }


        private ObservableCollection<FavoriteItem> _items;
        public ObservableCollection<FavoriteItem> Items
        {
            get { return _items; }
            set { _items = value; DoPropertyChanged("Items"); }
        }


        private bool _loadingLiveStatus;

        public bool LoaddingLiveStatus
        {
            get { return _loadingLiveStatus; }
            set { _loadingLiveStatus = value; DoPropertyChanged("LoaddingLiveStatus"); }
        }



        public async void LoadData(bool loadLiveStatus = true)
        {

            int maxConcurrencyLevel = SettingHelper.GetValue(SettingHelper.CONCURRENCY_LEVEL, 4);
            var semaphore = new SemaphoreSlim(maxConcurrencyLevel);

            try
            {
                Loading = true;
                foreach (var item in await DatabaseHelper.GetFavorites())
                {
                    Items.Add(item);
                }
                IsEmpty = Items.Count == 0;
                if (!IsEmpty && loadLiveStatus)
                {
                    LoadLiveStatus(semaphore);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            finally
            {
                semaphore.Release();
                Loading = false;
            }
        }

        public async void LoadLiveStatus(SemaphoreSlim semaphore)
        {
            LoaddingLiveStatus = true;
            Interlocked.Exchange(ref loadedCount, 0);
            var tasks = new List<Task>();
            foreach (var item in Items)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await LoadLiveStatusAsync(item, semaphore);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        }

        int loadedCount = 0;
        private async Task LoadLiveStatusAsync(FavoriteItem item, SemaphoreSlim semaphore)
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
                if (currentCount == Items.Count)
                {
                    // 切换到UI线程更新集合
                    await Dispatcher.RunOnUIThreadAsync(
                        () =>
                    {
                        LoaddingLiveStatus = false;
                        // 排序：直播 > 回放 > 未直播
                        Items = new ObservableCollection<FavoriteItem>(Items.OrderByDescending(x => (int)x.LiveStatus));
                    });
                }
            }
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
                IsEmpty = Items.Count == 0;
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

        }

        public async void Input()
        {

            // 打开文件选择器
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".json");
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.CommitButtonText = "导入";

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
                    Refresh();
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
