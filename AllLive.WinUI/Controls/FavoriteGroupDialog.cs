using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AllLive.WinUI.Helper;
using AllLive.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AllLive.WinUI.Controls
{
    /// <summary>
    /// 关注分组选择对话框：默认分组 / 已有分组 / 新建分组
    /// </summary>
    public static class FavoriteGroupDialog
    {
        /// <summary>
        /// “新建分组…”选项的占位 ID
        /// </summary>
        public const long NewGroupOptionID = -2;

        /// <summary>
        /// 展示选组对话框
        /// </summary>
        /// <param name="xamlRoot">所属窗口</param>
        /// <param name="groups">可选分组（不含“全部/默认分组”）</param>
        /// <param name="title">对话框标题</param>
        /// <param name="currentGroupId">当前分组，用于预选（null 表示默认分组）</param>
        public static async Task<FavoriteGroupSelectResult> Show(XamlRoot xamlRoot, List<FavoriteGroupItem> groups, string title, long? currentGroupId = null)
        {
            var options = new List<FavoriteGroupItem>()
            {
                new FavoriteGroupItem() { ID = -1, Name = "默认分组" }
            };
            options.AddRange(groups);
            options.Add(new FavoriteGroupItem() { ID = NewGroupOptionID, Name = "新建分组…" });

            var combo = new ComboBox()
            {
                ItemsSource = options,
                DisplayMemberPath = "Name",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 8)
            };
            // 预选当前分组
            int selectedIndex = 0;
            if (currentGroupId != null)
            {
                var idx = options.FindIndex(x => x.ID == currentGroupId.Value);
                if (idx > 0)
                {
                    selectedIndex = idx;
                }
            }
            combo.SelectedIndex = selectedIndex;

            var input = new TextBox()
            {
                PlaceholderText = "请输入新分组名称",
                Visibility = Visibility.Collapsed
            };
            combo.SelectionChanged += (s, e) =>
            {
                input.Visibility = (combo.SelectedItem as FavoriteGroupItem)?.ID == NewGroupOptionID
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };

            var panel = new StackPanel() { MinWidth = 320 };
            panel.Children.Add(new TextBlock() { Text = "选择分组" });
            panel.Children.Add(combo);
            panel.Children.Add(input);

            var result = new FavoriteGroupSelectResult() { IsConfirmed = false };
            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };
            dialog.PrimaryButtonClick += (s, e) =>
            {
                var selected = combo.SelectedItem as FavoriteGroupItem;
                if (selected == null)
                {
                    e.Cancel = true;
                    return;
                }
                if (selected.ID == NewGroupOptionID)
                {
                    var name = input.Text?.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        e.Cancel = true;
                        Utils.ShowMessageToast("请输入新分组名称", xamlRoot: xamlRoot);
                        return;
                    }
                    if (groups.Exists(x => x.Name == name))
                    {
                        e.Cancel = true;
                        Utils.ShowMessageToast("分组已存在", xamlRoot: xamlRoot);
                        return;
                    }
                    result.IsConfirmed = true;
                    result.NewGroupName = name;
                }
                else
                {
                    result.IsConfirmed = true;
                    result.GroupID = selected.ID == -1 ? (long?)null : selected.ID;
                }
            };

            var dialogResult = await dialog.ShowAsync();
            if (dialogResult != ContentDialogResult.Primary)
            {
                result.IsConfirmed = false;
            }
            return result;
        }
    }

    public class FavoriteGroupSelectResult
    {
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// 选中的分组 ID，null 表示默认分组
        /// </summary>
        public long? GroupID { get; set; }

        /// <summary>
        /// 新建分组名称（仅选择“新建分组…”时非空）
        /// </summary>
        public string NewGroupName { get; set; }
    }
}
