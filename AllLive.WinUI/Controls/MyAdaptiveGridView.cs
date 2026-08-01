using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AllLive.WinUI.Controls
{
    // Rewritten: GridView removed in CommunityToolkit 8.x, using GridView as base
    public class MyAdaptiveGridView : GridView
    {

        private ICommand _LoadMoreCommand;
        public ICommand LoadMoreCommand
        {
            get { return _LoadMoreCommand; }
            set { _LoadMoreCommand = value; }
        }
        public bool CanLoadMore { get; set; } = false;

        public double LoadMoreBottomOffset
        {
            get { return Convert.ToDouble(GetValue(LoadMoreBottomOffsetProperty)); }
            set { SetValue(LoadMoreBottomOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoadMoreBottomOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadMoreBottomOffsetProperty =
            DependencyProperty.Register("LoadMoreBottomOffset", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(100));






        public bool DataLoading
        {
            get { return (bool)GetValue(DataLoadingProperty); }
            set { SetValue(DataLoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Loading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataLoadingProperty =
            DependencyProperty.Register("DataLoading", typeof(bool), typeof(MyAdaptiveGridView), new PropertyMetadata(true));





        // Stub properties from AdaptiveGridView (removed in CommunityToolkit 8.x)
        public double DesiredWidth
        {
            get { return (double)GetValue(DesiredWidthProperty); }
            set { SetValue(DesiredWidthProperty, value); }
        }
        public static readonly DependencyProperty DesiredWidthProperty =
            DependencyProperty.Register("DesiredWidth", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(250.0));

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(0.0));

        public bool OneRowModeEnabled
        {
            get { return (bool)GetValue(OneRowModeEnabledProperty); }
            set { SetValue(OneRowModeEnabledProperty, value); }
        }
        public static readonly DependencyProperty OneRowModeEnabledProperty =
            DependencyProperty.Register("OneRowModeEnabled", typeof(bool), typeof(MyAdaptiveGridView), new PropertyMetadata(false));

        public bool StretchContentForSingleRow
        {
            get { return (bool)GetValue(StretchContentForSingleRowProperty); }
            set { SetValue(StretchContentForSingleRowProperty, value); }
        }
        public static readonly DependencyProperty StretchContentForSingleRowProperty =
            DependencyProperty.Register("StretchContentForSingleRow", typeof(bool), typeof(MyAdaptiveGridView), new PropertyMetadata(true));

        ScrollViewer scrollViewer;
        protected override void OnApplyTemplate()
        {
            scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            this.RegisterPropertyChangedCallback(DataLoadingProperty, new DependencyPropertyChangedCallback((obj, e) => {
                if (!DataLoading)
                {
                    if (scrollViewer.ScrollableHeight == 0)
                    {
                        LoadMoreCommand?.Execute(null);
                    }
                }
            }));

            base.OnApplyTemplate();
        }

        private void MyAdaptiveGridView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            Trace.WriteLine("���ݱ��");
            if (scrollViewer.ScrollableHeight == 0)
            {
                LoadMoreCommand?.Execute(null);
            }
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - LoadMoreBottomOffset && CanLoadMore)
            {
                LoadMoreCommand?.Execute(null);
            }

        }
    }
}
