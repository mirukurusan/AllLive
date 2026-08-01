using Windows.ApplicationModel;
using Microsoft.UI;
using AllLive.Core.Helper;
﻿using AllLive.WinUI.Controls;
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
    public sealed partial class RecomendPage : Page
    {
        readonly RecomendVM recomendVM;
        public RecomendPage()
        {
            recomendVM = new RecomendVM();

            this.InitializeComponent();
        }

        private void MyAdaptiveGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as AllLive.Core.Models.LiveRoomItem;
            if (item == null)
            {
                return;
            }

            var vm = (sender as MyAdaptiveGridView)?.DataContext as RecomendItemVM;
            if (vm?.site?.LiveSite == null)
            {
                return;
            }

            MessageCenter.OpenLiveRoom(vm.site.LiveSite, item);
        }

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivot.SelectedItem == null) return;
            var vm = pivot.SelectedItem as RecomendItemVM;
            if (vm.Loading == false && vm.Items.Count == 0)
            {
                vm.LoadData();
            }
        }
    }
}
