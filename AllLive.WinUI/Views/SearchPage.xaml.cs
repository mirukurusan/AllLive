using AllLive.Core.Models;
using AllLive.WinUI.Controls;
using AllLive.WinUI.Helper;
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
    public sealed partial class SearchPage : Page
    {
        readonly SearchVM searchVM;
        public SearchPage()
        {
            searchVM = new SearchVM();

            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MessageCenter.ChangeTitle("直播间搜索");
            if (e.NavigationMode == NavigationMode.New)
            {
                if (e.Parameter != null)
                {
                    searchBox.Text = e.Parameter.ToString();
                }
            }
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {

            }
            base.OnNavigatedFrom(e);
        }
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private void MyAdaptiveGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as LiveRoomItem;
            if (item == null)
            {
                return;
            }

            var vm = (sender as MyAdaptiveGridView)?.DataContext as SearchItemVM;
            if (vm?.site?.LiveSite == null)
            {
                return;
            }

            MessageCenter.OpenLiveRoom(vm.site.LiveSite, item);
        }

        private void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(searchBox.Text))
            {
                WinUIUtils.ShowMessageToast("关键字不能为空");
                return;
            }
            foreach (SearchItemVM item in pivot.Items)
            {
                item.Page = 1;
                item.Items.Clear();
            }
            (pivot.SelectedItem as SearchItemVM).LoadData(searchBox.Text);
        }

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivot.SelectedItem == null || string.IsNullOrEmpty(searchBox.Text)) return;
            var vm = pivot.SelectedItem as SearchItemVM;
            if (vm.Loading == false && vm.Items.Count == 0)
            {
                vm.LoadData(searchBox.Text);
            }
        }
    }
}
