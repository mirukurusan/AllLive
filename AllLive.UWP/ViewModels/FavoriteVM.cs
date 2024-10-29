using AllLive.Core.Models;
using AllLive.UWP.Helper;
using AllLive.UWP.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;

namespace AllLive.UWP.ViewModels
{
    public class FavoriteVM : BaseViewModel
    {
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

        public bool LoadingLiveStatus
        {
            get { return _loadingLiveStatus; }
            set { _loadingLiveStatus = value; DoPropertyChanged("LoadingLiveStatus"); }
        }

        public FavoriteVM()
        {
            Items = new ObservableCollection<FavoriteItem>();
            InputCommand = new RelayCommand(Input);
            OutputCommand = new RelayCommand(Output);
            TipCommand = new RelayCommand(Tip);
        }

        public async void LoadData()
        {
            try
            {
                Loading = true;
                LoadingLiveStatus = true;
                LoadingProgress = 0;
                var detailTasks = new List<Task>();
                var uiContext = SynchronizationContext.Current;
                await foreach (var item in DatabaseHelper.GetFavorites())
                {
                    item.Title = item.SiteName;
                    Items.Add(item);
                    detailTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var site = MainVM.Sites.Find(x => x.Name == item.SiteName);
                            var detail = await site.LiveSite.GetRoomDetail(item.RoomID);
                            uiContext.Post(state =>
                            {
                                if (detail.Status)
                                {
                                    item.Status = detail.Status;
                                    item.Cover = detail.Cover;
                                    if (!string.IsNullOrEmpty(detail.Title))
                                    {
                                        item.Title += $" - {detail.Title}";
                                    }
                                    if (!item.UserName.Equals(detail.UserName) || !item.Photo.Equals(detail.UserAvatar))
                                    {
                                        item.UserName = detail.UserName;
                                        item.Photo = detail.UserAvatar;
                                        DatabaseHelper.UpdateFavorite(item);
                                    }
                                }
                            }, null);

                        }
                        catch
                        {
                            uiContext.Post((state) =>
                            {
                                Utils.ShowMessageToast($"{item.UserName}的房间: {item.RoomID}，获取信息异常。");
                            }, null);
                        }
                    }));
                }
                while (detailTasks.Count > 0)
                {
                    var task = await Task.WhenAny(detailTasks);
                    detailTasks.Remove(task);
                    LoadingProgress += 1d / Items.Count;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            finally
            {
                IsEmpty = Items.Count == 0;
                LoadingProgress = 1;
                Loading = false;
                LoadingLiveStatus = false;
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
                    var items = JsonSerializer.Deserialize<List<FavoriteJsonItem>>(json);
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
                    Utils.ShowMessageToast("导入成功");
                    Refresh();
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    Utils.ShowMessageToast("导入失败");
                }
            }
        }

        public async void Output()
        {
            // 打开文件选择器
            FileSavePicker picker = new FileSavePicker();
            picker.FileTypeChoices.Add("Json", new List<string>() { ".json" });
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.SuggestedFileName = "AllLive.json";

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    var items = new List<FavoriteJsonItem>();
                    foreach (var item in Items)
                    {
                        var siteId = "";
                        switch (item.SiteName)
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
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        IncludeFields = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    var json = JsonSerializer.Serialize(items, options);
                    await FileIO.WriteTextAsync(file, json);
                    Utils.ShowMessageToast("导出成功");
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    Utils.ShowMessageToast("导出失败");
                }
            }


        }

        public void Tip()
        {
            MessageDialog dialog = new MessageDialog(@"该程序兼容Simple Live，您可以导入Simple Live的关注数据，导出的数据也可以在Simple Live中导入。", "导入导出说明");
            _ = dialog.ShowAsync();
        }
    }

    public class FavoriteJsonItem
    {
        [JsonPropertyName("siteId")]
        public string SiteId;

        [JsonPropertyName("id")]
        public string Id;

        [JsonPropertyName("roomId")]
        public string RoomId;

        [JsonPropertyName("userName")]
        public string UserName;

        [JsonPropertyName("face")]
        public string Face;

        [JsonPropertyName("addTime")]
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
