using System;
using System.Collections.ObjectModel;
using AllLive.Core.Interface;
using AllLive.Core.Models;

namespace AllLive.WinUI.ViewModels
{
    public class CategoryDetailVM:BaseViewModel
    {
        public CategoryDetailVM()
        {
            Items = new ObservableCollection<LiveRoomItem>();
        }

        public ObservableCollection<LiveRoomItem> Items { get; set; }

        
        private ILiveSite _site;
        public ILiveSite Site
        {
            get { return _site; }
            set { _site = value; }
        }
        private LiveSubCategory _category;
        public LiveSubCategory Category
        {
            get { return _category; }
            set { _category = value; }
        }

        public async void LoadData(ILiveSite site, LiveSubCategory category)
        {
            try
            {
                Site = site;
                Category = category;
                Loading = true;
                CanLoadMore = false;
                IsEmpty = false;
                var result = await site.GetCategoryRooms(category, Page);
                foreach (var item in result.Rooms)
                {
                    Items.Add(item);
                }
                IsEmpty = Items.Count == 0;
                CanLoadMore = result.HasMore;
                Page++;
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

        public override void Refresh()
        {
            base.Refresh();
            Items.Clear();
            LoadData(_site, _category);
        }

        public override void LoadMore()
        {
            base.LoadMore();
            LoadData(_site, _category);
        }
    }
}
