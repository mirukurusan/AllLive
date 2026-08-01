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
    public sealed partial class CategoryDetailPage : Page
    {
        readonly CategoryDetailVM categoryDetailVM;
        PageArgs pageArgs;
        public CategoryDetailPage()
        {
            categoryDetailVM = new CategoryDetailVM();
            this.InitializeComponent();

        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.New)
            {
                pageArgs = e.Parameter as PageArgs;
                var category = pageArgs.Data as LiveSubCategory;
                MessageCenter.ChangeTitle(category.Name, pageArgs.Site);
                //txtTitle.Text = pageArgs.Site.Name+" - " +category.Name;
                categoryDetailVM.LoadData(pageArgs.Site, category);
            }
            else if (e.NavigationMode == NavigationMode.Back)
            {
                MessageCenter.ChangeTitle(categoryDetailVM.Category.Name, categoryDetailVM.Site);

            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (e.NavigationMode == NavigationMode.Back)
            {

            }
        }

        private void MyAdaptiveGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as AllLive.Core.Models.LiveRoomItem;
            if (item == null || pageArgs?.Site == null)
            {
                return;
            }

            MessageCenter.OpenLiveRoom(pageArgs.Site, item);

        }
    }
}
