using System;

namespace AllLive.WinUI.Models
{
    public class HistoryItem
    {
        public int ID { get; set; }
        public string RoomID { get; set; }
        public string UserName { get; set; }
        public string Photo { get; set; }
        public string SiteName { get; set; }
        public DateTime WatchTime { get; set; }
    }
}
