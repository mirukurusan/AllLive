using Windows.ApplicationModel;
using Microsoft.UI;
using AllLive.Core.Helper;
﻿using AllLive.Core.Models;
using AllLive.WinUI.Helper;
using AllLive.WinUI.Models;
using AllLive.WinUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace AllLive.WinUI.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class CategoryPage : Page
    {
        readonly CategoryVM categoryVM;
        public CategoryPage()
        {
            categoryVM = new CategoryVM();

            this.InitializeComponent();
        }
        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivot.SelectedItem == null) return;
            var vm = pivot.SelectedItem as CategoryItemVM;
            if (vm.Loading == false && vm.CollectionView == null)
            {
                vm.LoadData();
            }
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as LiveSubCategory;
            if (item == null)
            {
                return;
            }

            var vm = pivot.SelectedItem as CategoryItemVM;
            if (vm?.site?.LiveSite == null)
            {
                return;
            }

            MessageCenter.NavigatePage(typeof(CategoryDetailPage), new PageArgs()
            {
                Site = vm.site.LiveSite,
                Data = item
            });
        }
    }
}
