using System.Collections.ObjectModel;
using AllLive.WinUI.ViewModels;

namespace AllLive.WinUI.Models
{
    /// <summary>
    /// 关注分组
    /// </summary>
    public class FavoriteGroupItem : BaseNotifyPropertyChanged
    {
        /// <summary>
        /// 0 表示“全部”，-1 表示默认分组，大于 0 为数据库分组
        /// </summary>
        public long ID { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; DoPropertyChanged("Name"); }
        }

        public int SortOrder { get; set; }

        /// <summary>
        /// 特殊分组（全部/默认分组），不可删除、不可重命名
        /// </summary>
        public bool IsSpecial { get; set; }

        /// <summary>
        /// 组内主播集合
        /// </summary>
        public ObservableCollection<FavoriteItem> Items { get; set; } = new ObservableCollection<FavoriteItem>();
    }
}
