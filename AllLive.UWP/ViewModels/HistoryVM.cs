using AllLive.UWP.Helper;
using AllLive.UWP.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AllLive.UWP.ViewModels
{
    public class HistoryVM : BaseViewModel
    {
        public HistoryVM()
        {
            Items = new ObservableCollection<HistoryItem>();
            CleanCommand = new RelayCommand(Clean);
        }
        public ICommand CleanCommand { get; set; }

        public ObservableCollection<HistoryItem> Items { get; set; }

        private bool _loadingLiveStatus;

        public bool LoadingLiveStatus
        {
            get { return _loadingLiveStatus; }
            set { _loadingLiveStatus = value; DoPropertyChanged("LoadingLiveStatus"); }
        }

        public async void LoadData()
        {
            int maxConcurrencyLevel = SettingHelper.GetValue(SettingHelper.CONCURRENCY_LEVEL, 4);
            var semaphore = new SemaphoreSlim(maxConcurrencyLevel);

            try
            {
                Loading = true;
                LoadingLiveStatus = true;
                LoadingProgress = 0;
                var detailTasks = new List<Task>();
                var uiContext = SynchronizationContext.Current;
                await foreach (var item in DatabaseHelper.GetHistory())
                {
                    item.Title = item.SiteName;
                    Items.Add(item);
                }
                foreach (var item in Items)
                {
                    await semaphore.WaitAsync();
                    detailTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var site = MainVM.Sites.Find(x => x.Name == item.SiteName);
                            if (site == null) return;
                            var detail = await site.LiveSite.GetRoomDetail(item.RoomID);
                            uiContext.Post(state =>
                            {
                                if (detail.Status)
                                {
                                    item.Status = detail.Status;
                                    if (!string.IsNullOrEmpty(detail.Title))
                                    {
                                        item.Title += $" - {detail.Title}";
                                    }
                                    if (!item.UserName.Equals(detail.UserName) || !item.Photo.Equals(detail.UserAvatar))
                                    {
                                        item.UserName = detail.UserName;
                                        item.Photo = detail.UserAvatar;
                                        DatabaseHelper.UpdateHistory(item);
                                    }
                                }
                            }, null);
                        }
                        catch (Exception ex)
                        {
                            uiContext.Post((state) =>
                            {
                                Utils.ShowMessageToast($"{item.UserName}的房间: {item.RoomID}，获取信息异常。\n{ex.Message}");
                            }, null);
                        }
                        finally
                        {
                            semaphore.Release();
                            uiContext.Post(_ =>
                            {
                                LoadingProgress += 1.0 / Items.Count;
                            }, null);
                        }
                    }));
                }
                await Task.WhenAll(detailTasks);
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

        public void RemoveItem(HistoryItem item)
        {
            try
            {
                DatabaseHelper.DeleteHistory(item.ID);
                Items.Remove(item);
                IsEmpty = Items.Count == 0;
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

        }

        public async void Clean()
        {
            try
            {

                var result = await Utils.ShowDialog("清空记录", $"确定要清除全部观看记录吗?");
                if (!result)
                {
                    return;
                }

                DatabaseHelper.DeleteHistory();
                Items.Clear();
                IsEmpty = true;
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }
    }
}
