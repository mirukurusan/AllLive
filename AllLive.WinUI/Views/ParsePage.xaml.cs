using System;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using AllLive.Core.Helper;
using AllLive.WinUI.Models;
using AllLive.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AllLive.WinUI.Views
{
    public sealed partial class ParsePage : Page
    {
        public ParsePage()
        {
            this.InitializeComponent();
            cmbSite.SelectedIndex = 0;
        }

        private async void BtnPaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var content = Clipboard.GetContent();
                if (content.Contains(StandardDataFormats.Text))
                {
                    txtInput.Text = await content.GetTextAsync();
                }
            }
            catch { }
        }

        private void BtnGo_Click(object sender, RoutedEventArgs e)
        {
            var input = txtInput.Text?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                txtStatus.Text = "请输入链接或房间号";
                return;
            }

            var selectedTag = (cmbSite.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto";
            
            Site site = null;
            string roomId = null;

            if (selectedTag == "auto")
            {
                (site, roomId) = ParseInput(input);
            }
            else
            {
                LiveSite siteType;
                switch (selectedTag)
                {
                    case "douyin":
                        siteType = LiveSite.Douyin;
                        break;
                    case "bilibili":
                        siteType = LiveSite.Bilibili;
                        break;
                    case "huya":
                        siteType = LiveSite.Huya;
                        break;
                    case "douyu":
                        siteType = LiveSite.Douyu;
                        break;
                    default:
                        siteType = LiveSite.Douyin;
                        break;
                }
                site = MainVM.Sites.FirstOrDefault(s => s.SiteType == siteType);
                roomId = ExtractRoomId(input);
            }

            if (site != null && !string.IsNullOrEmpty(roomId))
            {
                txtStatus.Text = $"正在进入 {site.Name} - {roomId}";
                NavigateToRoom(site, roomId);
            }
            else
            {
                txtStatus.Text = "无法识别，请选择平台或检查输入";
            }
        }

        private (Site site, string roomId) ParseInput(string input)
        {
            // 抖音
            var douyinMatch = Regex.Match(input, @"live\.douyin\.com/(\d+)");
            if (douyinMatch.Success)
            {
                return (MainVM.Sites.FirstOrDefault(s => s.SiteType == LiveSite.Douyin), 
                        douyinMatch.Groups[1].Value);
            }

            // B站
            var biliMatch = Regex.Match(input, @"live\.bilibili\.com/(\d+)");
            if (biliMatch.Success)
            {
                return (MainVM.Sites.FirstOrDefault(s => s.SiteType == LiveSite.Bilibili), 
                        biliMatch.Groups[1].Value);
            }

            // 虎牙
            var huyaMatch = Regex.Match(input, @"huya\.com/(\w+)");
            if (huyaMatch.Success)
            {
                return (MainVM.Sites.FirstOrDefault(s => s.SiteType == LiveSite.Huya), 
                        huyaMatch.Groups[1].Value);
            }

            // 斗鱼
            var douyuMatch = Regex.Match(input, @"douyu\.com/(\d+)");
            if (douyuMatch.Success)
            {
                return (MainVM.Sites.FirstOrDefault(s => s.SiteType == LiveSite.Douyu), 
                        douyuMatch.Groups[1].Value);
            }

            // 纯数字默认抖音
            if (Regex.IsMatch(input, @"^\d+$"))
            {
                return (MainVM.Sites.FirstOrDefault(s => s.SiteType == LiveSite.Douyin), input);
            }

            return (null, null);
        }

        private string ExtractRoomId(string input)
        {
            var match = Regex.Match(input, @"/(\w+)/?$");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return Regex.Replace(input, @"[^\w]", "");
        }

        private void NavigateToRoom(Site site, string roomId)
        {
            this.Frame.Navigate(typeof(LiveRoomPage), new PageArgs
            {
                Site = site.LiveSite,
                Data = roomId
            });
        }
    }
}
