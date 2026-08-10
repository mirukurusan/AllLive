using System;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using AllLive.WinUI.Controls;
using AllLive.WinUI.Helper;
using AllLive.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using WinUIUtils = AllLive.WinUI.Helper.Utils;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace AllLive.WinUI.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        readonly SettingVM settingVM;
        public SettingsPage()
        {
            settingVM = new SettingVM();
            this.InitializeComponent();
            BiliAccount.Instance.OnAccountChanged += BiliAccount_OnAccountChanged;
            LoadUI();

            // 页面卸载时取消事件订阅
            this.Unloaded += SettingsPage_Unloaded;
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            BiliAccount.Instance.OnAccountChanged -= BiliAccount_OnAccountChanged;
            this.Unloaded -= SettingsPage_Unloaded;
        }

        private void BiliAccount_OnAccountChanged(object sender, EventArgs e)
        {
            if (BiliAccount.Instance.Logined)
            {
                txtBili.Text = $"已登录：{BiliAccount.Instance.UserName}";
                BtnLoginBili.Visibility = Visibility.Collapsed;
                BtnLogoutBili.Visibility = Visibility.Visible;
            }
            else
            {
                txtBili.Text = "登录可享受高清直播";
                BtnLoginBili.Visibility = Visibility.Visible;
                BtnLogoutBili.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadUI()
        {
            //主题
            cbTheme.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.THEME, 0);
            cbTheme.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbTheme.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.THEME, cbTheme.SelectedIndex);
                    var window = App.GetMainWindow();
                    if (window != null)
                    {
                        Frame rootFrame = window.Content as Frame;
                        switch (cbTheme.SelectedIndex)
                        {
                            case 1:
                                rootFrame.RequestedTheme = ElementTheme.Light;
                                break;
                            case 2:
                                rootFrame.RequestedTheme = ElementTheme.Dark;
                                break;
                            default:
                                rootFrame.RequestedTheme = ElementTheme.Default;
                                break;
                        }
                        App.SetTitleBar();

                    }
                });
            });

            //导航栏显示模式
            cbPaneDisplayMode.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.PANE_DISPLAY_MODE, 0);
            cbPaneDisplayMode.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbPaneDisplayMode.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.PANE_DISPLAY_MODE, cbPaneDisplayMode.SelectedIndex);
                    MessageCenter.UpdatePanelDisplayMode();
                });
            });

            //鼠标侧键返回
            swMouseClosePage.IsOn = SettingHelper.GetValue<bool>(SettingHelper.MOUSE_BACK, true);
            swMouseClosePage.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swMouseClosePage.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.MOUSE_BACK, swMouseClosePage.IsOn);
                });
            });

            //关注加载线程数
            concurrencyLevel.Value = SettingHelper.GetValue<int>(SettingHelper.CONCURRENCY_LEVEL, 3);
            concurrencyLevel.Loaded += new RoutedEventHandler((sender, e) =>
            {
                concurrencyLevel.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.CONCURRENCY_LEVEL, Convert.ToInt32(args.NewValue));
                });
            });

            //视频解码
            cbDecoder.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.VIDEO_DECODER, 0);
            cbDecoder.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbDecoder.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.VIDEO_DECODER, cbDecoder.SelectedIndex);
                });
            });

            numFontsize.Value = SettingHelper.GetValue<double>(SettingHelper.MESSAGE_FONTSIZE, 14.0);
            numFontsize.Loaded += new RoutedEventHandler((sender, e) =>
            {
                numFontsize.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.MESSAGE_FONTSIZE, args.NewValue);
                });
            });

            //默认清晰度
            quality.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.VIDEO_QUALITY, 0);
            quality.Loaded += new RoutedEventHandler((sender, e) =>
            {
                quality.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.VIDEO_QUALITY, quality.SelectedIndex);
                });
            });
            //数据网络默认清晰度
            meteredQuality.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.VIDEO_QUALITY_METERED, 0);
            meteredQuality.Loaded += new RoutedEventHandler((sender, e) =>
            {
                meteredQuality.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.VIDEO_QUALITY_METERED, meteredQuality.SelectedIndex);
                });
            });

            //新窗口打开
            swNewWindow.IsOn = SettingHelper.GetValue<bool>(SettingHelper.NEW_WINDOW_LIVEROOM, false);
            swNewWindow.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swNewWindow.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.NEW_WINDOW_LIVEROOM, swNewWindow.IsOn);
                });
            });
            //默认开启直播录制
            swAutoRecord.IsOn = SettingHelper.GetValue<bool>(SettingHelper.AUTO_START_RECORDING, false);
            swAutoRecord.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swAutoRecord.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.AUTO_START_RECORDING, swAutoRecord.IsOn);
                });
            });
            //录制保存格式
            cbRecordFormat.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.RECORD_FORMAT, 0);
            cbRecordFormat.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbRecordFormat.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.RECORD_FORMAT, cbRecordFormat.SelectedIndex);
                });
            });
            //弹幕开关
            var state = SettingHelper.GetValue<bool>(SettingHelper.LiveDanmaku.SHOW, true);
            DanmuSettingState.IsOn = state;
            DanmuSettingState.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue(SettingHelper.LiveDanmaku.SHOW, DanmuSettingState.IsOn);
            });

            // 保留醒目留言
            var keepSC = SettingHelper.GetValue<bool>(SettingHelper.LiveDanmaku.KEEP_SUPER_CHAT, true);
            SettingKeepSC.IsOn = keepSC;
            SettingKeepSC.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue(SettingHelper.LiveDanmaku.KEEP_SUPER_CHAT, SettingKeepSC.IsOn);
            });

            //弹幕清理
            numCleanCount.Value = SettingHelper.GetValue<int>(SettingHelper.LiveDanmaku.DANMU_CLEAN_COUNT, 200);
            numCleanCount.Loaded += new RoutedEventHandler((sender, e) =>
            {
                numCleanCount.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.LiveDanmaku.DANMU_CLEAN_COUNT, Convert.ToInt32(args.NewValue));
                });
            });
            //弹幕关键词
            LiveDanmuSettingListWords.ItemsSource = settingVM.ShieldWords;


            if(BiliAccount.Instance.Logined)
            {
                txtBili.Text = $"已登录：{BiliAccount.Instance.UserName}";
                BtnLoginBili.Visibility = Visibility.Collapsed;
                BtnLogoutBili.Visibility = Visibility.Visible;
            }

            if (DouyinAccount.Instance.Logined)
            {
                txtDouyin.Text = "已登录";
                BtnLoginDouyin.Visibility = Visibility.Collapsed;
                BtnLogoutDouyin.Visibility = Visibility.Visible;
            }

        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            version.Text = WinUIUtils.GetAppVersion().ToString(3);
        }
        private void RemoveLiveDanmuWord_Click(object sender, RoutedEventArgs e)
        {
            var word = (sender as AppBarButton).DataContext as string;
            settingVM.ShieldWords.Remove(word);
            SettingHelper.SetValue(SettingHelper.LiveDanmaku.SHIELD_WORD, JsonConvert.SerializeObject(settingVM.ShieldWords));
        }

        private void LiveDanmuSettingTxtWord_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(LiveDanmuSettingTxtWord.Text))
            {
                WinUIUtils.ShowMessageToast("关键字不能为空");
                return;
            }
            if (!settingVM.ShieldWords.Contains(LiveDanmuSettingTxtWord.Text))
            {
                settingVM.ShieldWords.Add(LiveDanmuSettingTxtWord.Text);
                SettingHelper.SetValue(SettingHelper.LiveDanmaku.SHIELD_WORD, JsonConvert.SerializeObject(settingVM.ShieldWords));
            }

            LiveDanmuSettingTxtWord.Text = "";
            SettingHelper.SetValue(SettingHelper.LiveDanmaku.SHIELD_WORD, JsonConvert.SerializeObject(settingVM.ShieldWords));
        }

        private async void BtnGithub_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/xiaoyaocz/AllLive"));
        }

        private async void BtnLog_Click(object sender, RoutedEventArgs e)
        {
            var storageFolder = await WinUIUtils.GetLocalFolderAsync();
            var logFolder = await storageFolder.CreateFolderAsync("log", CreationCollisionOption.OpenIfExists);
            await Launcher.LaunchFolderAsync(logFolder);
        }

        private async void BtnLoginBili_Click(object sender, RoutedEventArgs e)
        {
            if (BiliAccount.Instance.Logined)
            {
                WinUIUtils.ShowMessageToast("已登录");
                return;
            }
            var result= await MessageCenter.BiliBiliLogin();
            if (result)
            {
                txtBili.Text = $"已登录：{BiliAccount.Instance.UserName}";
                BtnLoginBili.Visibility = Visibility.Collapsed;
                BtnLogoutBili.Visibility = Visibility.Visible;
            }
        }

        private void BtnLogoutBili_Click(object sender, RoutedEventArgs e)
        {
            BiliAccount.Instance.Logout();
            txtBili.Text = "登录可享受高清直播";
            BtnLoginBili.Visibility = Visibility.Visible;
            BtnLogoutBili.Visibility = Visibility.Collapsed;

        }

        private async void BtnLoginDouyin_Click(object sender, RoutedEventArgs e)
        {
            if (DouyinAccount.Instance.Logined)
            {
                WinUIUtils.ShowMessageToast("已登录");
                return;
            }
            var dialog = new DouyinLoginDialog()
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            if (dialog.LoginSuccess)
            {
                txtDouyin.Text = "已登录";
                BtnLoginDouyin.Visibility = Visibility.Collapsed;
                BtnLogoutDouyin.Visibility = Visibility.Visible;
            }
        }

        private void BtnLogoutDouyin_Click(object sender, RoutedEventArgs e)
        {
            DouyinAccount.Instance.Logout();
            txtDouyin.Text = "登录后可搜索直播间";
            BtnLoginDouyin.Visibility = Visibility.Visible;
            BtnLogoutDouyin.Visibility = Visibility.Collapsed;
        }
    }
}
