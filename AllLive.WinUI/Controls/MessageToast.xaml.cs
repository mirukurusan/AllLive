using System;
using System.Collections.Generic;
using System.Numerics;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace AllLive.WinUI.Controls
{
    public sealed partial class MessageToast : UserControl
    {
        private Popup m_Popup;

        private string m_TextBlockContent = "";
        private TimeSpan m_ShowTime;
        // toast显示的窗口
        private XamlRoot m_XamlRoot;

        public MessageToast() : this(null)
        {
        }

        public MessageToast(XamlRoot xamlRoot)
        {
            InitializeComponent();
            m_XamlRoot = xamlRoot;
            m_Popup = new Popup();
            if (m_XamlRoot != null)
            {
                Width = m_XamlRoot.Size.Width;
                Height = m_XamlRoot.Size.Height;
                m_Popup.XamlRoot = m_XamlRoot;
            }
            else
            {
                var window = App.GetMainWindow();
                Width = window != null ? window.Bounds.Width : 1920;
                Height = window != null ? window.Bounds.Height : 1080;
            }
            m_Popup.Child = this;
            Loaded += NotifyPopup_Loaded;
            Unloaded += NotifyPopup_Unloaded;
        }

        public MessageToast(string content, TimeSpan showTime, XamlRoot xamlRoot) : this(xamlRoot)
        {
            if (m_TextBlockContent == null)
            {
                m_TextBlockContent = "";
            }
            this.m_TextBlockContent = content;
            this.m_ShowTime = showTime;
        }
        public MessageToast(string content, TimeSpan showTime, List<MyUICommand> commands) : this(content, showTime, (XamlRoot)null)
        {
            if (m_TextBlockContent == null)
            {
                m_TextBlockContent = "";
            }
            this.m_TextBlockContent = content;
            this.m_ShowTime = showTime;
            foreach (var item in commands)
            {
                HyperlinkButton button = new HyperlinkButton()
                {
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = new TextBlock() { Text = item.Label }
                };
                button.Click += new RoutedEventHandler((sender, e) => {
                    item.Invoked?.Invoke(this, item);
                });
                btns.Children.Add(button);
            }
        }


        public void Show()
        {
            if (m_Popup.XamlRoot == null)
            {
                m_Popup.XamlRoot = App.GetMainWindow()?.Content?.XamlRoot;
            }
            this.m_Popup.IsOpen = true;

        }
        public async void Close()
        {
            await AnimationBuilder.Create().Offset(to: new Vector2(0, (float)border.ActualHeight), duration: TimeSpan.FromMilliseconds(200)).StartAsync(this);

           // await this.Offset(offsetX: 0, offsetY: (float)border.ActualHeight, duration: 200, delay: 0, easingType: EasingType.Default).StartAsync();
            this.m_Popup.IsOpen = false;
        }
        private async void NotifyPopup_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_TextBlockContent == null)
            {
                m_TextBlockContent = "";
            }
            this.tbNotify.Text = m_TextBlockContent;
            if (m_XamlRoot != null)
            {
                m_XamlRoot.Changed += Current_XamlRoot_Changed;
            }
            else
            {
                var window = App.GetMainWindow();
                if (window != null)
                {
                    window.SizeChanged += Current_SizeChanged;
                }
            }
            await AnimationBuilder.Create().Offset(to: new Vector2(0, -72), duration: TimeSpan.FromMilliseconds(200)).StartAsync(this);
            //await this.Offset(offsetX: 0, offsetY: -72, duration: 200, delay: 0, easingType: EasingType.Default).StartAsync();
            await AnimationBuilder.Create().Offset(to: new Vector2(0, (float)border.ActualHeight), duration: TimeSpan.FromMilliseconds(200), delay: TimeSpan.FromMilliseconds(m_ShowTime.TotalMilliseconds)).StartAsync(this);
            //await this.Offset(offsetX: 0, offsetY: (float)border.ActualHeight, duration: 200, delay: m_ShowTime.TotalMilliseconds, easingType: EasingType.Default).StartAsync();
            this.m_Popup.IsOpen = false;
        }


        private void Current_XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            this.Width = sender.Size.Width;
            this.Height = sender.Size.Height;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            this.Width = e.Size.Width;
            this.Height = e.Size.Height;
        }

        private void NotifyPopup_Unloaded(object sender, RoutedEventArgs e)
        {
            if (m_XamlRoot != null)
            {
                m_XamlRoot.Changed -= Current_XamlRoot_Changed;
            }
            else
            {
                var window = App.GetMainWindow();
                if (window != null)
                {
                    window.SizeChanged -= Current_SizeChanged;
                }
            }
        }


    }
    public class MyUICommand
    {
        public MyUICommand(string lable)
        {
            Label = lable;
        }
        public MyUICommand(string lable, EventHandler<MyUICommand> invoked)
        {
            Label = lable;
            Invoked = invoked;
        }
        public object Id { get; set; }
        public EventHandler<MyUICommand> Invoked { get; set; }
        public string Label { get; set; }


    }
}
