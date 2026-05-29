using AllLive.Core.Models;
using AllLive.UWP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllLive.UWP.Models
{
    public class FavoriteItem: BaseNotifyPropertyChanged
    {
        public int ID { get; set; }
        public string RoomID { get; set; }
        public string UserName { get; set; }
        public string Photo { get; set; }
        public string SiteName { get; set; }


        private LiveStatusType _LiveStatus = LiveStatusType.Offline;
        public LiveStatusType LiveStatus
        {
            get { return _LiveStatus; }
            set
            {
                _LiveStatus = value;
                DoPropertyChanged("LiveStatus");
                DoPropertyChanged("IsLive");
                DoPropertyChanged("IsReplay");
                DoPropertyChanged("IsLiveOrReplay");
                DoPropertyChanged("IsOffline");
            }
        }

        private bool _StatusLoaded = false;
        public bool StatusLoaded
        {
            get { return _StatusLoaded; }
            set
            {
                _StatusLoaded = value;
                DoPropertyChanged("StatusLoaded");
                DoPropertyChanged("IsOffline");
            }
        }

        private bool _IsLoadingStatus = false;
        public bool IsLoadingStatus
        {
            get { return _IsLoadingStatus; }
            set
            {
                _IsLoadingStatus = value;
                DoPropertyChanged("IsLoadingStatus");
                DoPropertyChanged("IsLoading");
            }
        }

        /// <summary>
        /// 是否正在直播
        /// </summary>
        public bool IsLive => LiveStatus == LiveStatusType.Live;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading => IsLoadingStatus;

        /// <summary>
        /// 是否未开播
        /// </summary>
        public bool IsOffline => StatusLoaded && LiveStatus == LiveStatusType.Offline;
    }
}
