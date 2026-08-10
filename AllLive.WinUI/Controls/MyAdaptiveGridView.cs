using System;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace AllLive.WinUI.Controls
{
    // 自适应网格：由 CommunityToolkit.Uwp.UI.Controls.AdaptiveGridView (v7.1) 移植而来，
    // 用于在 CommunityToolkit 8.x 移除 AdaptiveGridView 之后恢复自适应尺寸计算。
    public class MyAdaptiveGridView : GridView
    {
        private bool _needContainerMarginForLayout;

        private ScrollViewer scrollViewer;

        public MyAdaptiveGridView()
        {
            IsTabStop = false;
            UseLayoutRounding = false;
            SizeChanged += OnGridSizeChanged;
            Items.VectorChanged += ItemsOnVectorChanged;
        }

        // ============ 自适应尺寸属性 ============

        private static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register("ItemWidth", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(double.NaN));

        public static readonly DependencyProperty DesiredWidthProperty =
            DependencyProperty.Register("DesiredWidth", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(double.NaN, OnDesiredWidthChanged));

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(double.NaN));

        public static readonly DependencyProperty OneRowModeEnabledProperty =
            DependencyProperty.Register("OneRowModeEnabled", typeof(bool), typeof(MyAdaptiveGridView), new PropertyMetadata(false));

        public static readonly DependencyProperty StretchContentForSingleRowProperty =
            DependencyProperty.Register("StretchContentForSingleRow", typeof(bool), typeof(MyAdaptiveGridView), new PropertyMetadata(true, OnStretchContentForSingleRowChanged));

        public double DesiredWidth
        {
            get { return (double)GetValue(DesiredWidthProperty); }
            set { SetValue(DesiredWidthProperty, value); }
        }

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        public bool OneRowModeEnabled
        {
            get { return (bool)GetValue(OneRowModeEnabledProperty); }
            set { SetValue(OneRowModeEnabledProperty, value); }
        }

        public bool StretchContentForSingleRow
        {
            get { return (bool)GetValue(StretchContentForSingleRowProperty); }
            set { SetValue(StretchContentForSingleRowProperty, value); }
        }

        private double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        // ============ 加载更多（保留原有逻辑） ============

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

        public static readonly DependencyProperty LoadMoreBottomOffsetProperty =
            DependencyProperty.Register("LoadMoreBottomOffset", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(100));

        public bool DataLoading
        {
            get { return (bool)GetValue(DataLoadingProperty); }
            set { SetValue(DataLoadingProperty, value); }
        }

        public static readonly DependencyProperty DataLoadingProperty =
            DependencyProperty.Register("DataLoading", typeof(bool), typeof(MyAdaptiveGridView), new PropertyMetadata(true));

        // ============ 生命周期 ============

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            }

            RegisterPropertyChangedCallback(DataLoadingProperty, (obj, e) =>
            {
                if (!DataLoading && scrollViewer != null && scrollViewer.ScrollableHeight == 0)
                {
                    LoadMoreCommand?.Execute(null);
                }
            });
        }

        protected override void PrepareContainerForItemOverride(DependencyObject obj, object item)
        {
            base.PrepareContainerForItemOverride(obj, item);

            if (obj is FrameworkElement frameworkElement)
            {
                frameworkElement.SetBinding(HeightProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath("ItemHeight"),
                    Mode = BindingMode.TwoWay
                });
                frameworkElement.SetBinding(WidthProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath("ItemWidth"),
                    Mode = BindingMode.TwoWay
                });
            }

            if (obj is ContentControl contentControl)
            {
                contentControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                contentControl.VerticalContentAlignment = VerticalAlignment.Stretch;
            }

            if (_needContainerMarginForLayout)
            {
                _needContainerMarginForLayout = false;
                RecalculateLayout(ActualWidth);
            }
        }

        // ============ 尺寸计算 ============

        private void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Width != e.NewSize.Width)
            {
                RecalculateLayout(e.NewSize.Width);
            }
        }

        private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            if (!double.IsNaN(ActualWidth))
            {
                RecalculateLayout(ActualWidth);
            }
        }

        private static void OnDesiredWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as MyAdaptiveGridView;
            if (grid != null)
            {
                grid.RecalculateLayout(grid.ActualWidth);
            }
        }

        private static void OnStretchContentForSingleRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as MyAdaptiveGridView;
            if (grid != null)
            {
                grid.RecalculateLayout(grid.ActualWidth);
            }
        }

        private void RecalculateLayout(double containerWidth)
        {
            Panel itemsPanelRoot = ItemsPanelRoot;
            double panelMargin = itemsPanelRoot != null ? itemsPanelRoot.Margin.Left + itemsPanelRoot.Margin.Right : 0.0;
            double padding = Padding.Left + Padding.Right;
            double border = BorderThickness.Left + BorderThickness.Right;

            containerWidth = containerWidth - padding - panelMargin - border;
            if (containerWidth > 0.0)
            {
                ItemWidth = Math.Floor(CalculateItemWidth(containerWidth));
            }
        }

        private double CalculateItemWidth(double containerWidth)
        {
            if (double.IsNaN(DesiredWidth))
            {
                return DesiredWidth;
            }

            int columns = CalculateColumns(containerWidth, DesiredWidth);
            if (Items != null && Items.Count > 0 && Items.Count < columns && StretchContentForSingleRow)
            {
                columns = Items.Count;
            }

            Thickness itemMargin = GetItemMargin(ItemContainerStyle);
            if (itemMargin == default(Thickness))
            {
                _needContainerMarginForLayout = true;
            }

            return (containerWidth / columns) - itemMargin.Left - itemMargin.Right;
        }

        private static int CalculateColumns(double containerWidth, double itemWidth)
        {
            int columns = (int)Math.Round(containerWidth / itemWidth);
            if (columns == 0)
            {
                columns = 1;
            }
            return columns;
        }

        private static Thickness GetItemMargin(Style style)
        {
            while (style != null)
            {
                foreach (var setter in style.Setters)
                {
                    if (setter is Setter s && s.Property == MarginProperty && s.Value is Thickness t)
                    {
                        return t;
                    }
                }
                style = style.BasedOn;
            }
            return default(Thickness);
        }

        // ============ 滚动加载更多 ============

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (scrollViewer != null &&
                scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - LoadMoreBottomOffset &&
                CanLoadMore)
            {
                LoadMoreCommand?.Execute(null);
            }
        }
    }
}
