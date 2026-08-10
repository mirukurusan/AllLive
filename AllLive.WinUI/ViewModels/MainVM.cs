using System.Collections.Generic;
using AllLive.Core;
using AllLive.Core.Helper;
using AllLive.Core.Interface;

namespace AllLive.WinUI.ViewModels
{
    public class MainVM
    {

        public static List<Site> Sites = new List<Site>() {
            new Site()
            {
                SiteType=LiveSite.Bilibili,
                Name="哔哩哔哩直播",
                Logo="ms-appx:///Assets/Logo/bilibili.png",
                LiveSite=new BiliBili(),
            },
            new Site()
            {
                SiteType=LiveSite.Douyu,
                Name="斗鱼直播",
                Logo="ms-appx:///Assets/Logo/douyu.png",
                LiveSite=new Douyu(),
            },
            new Site()
            {
                SiteType=LiveSite.Huya,
                Name="虎牙直播",
                Logo="ms-appx:///Assets/Logo/huya.png",
                LiveSite=new Huya(),
            },
            new Site()
            {
                SiteType=LiveSite.Douyin,
                Name="抖音直播",
                Logo="ms-appx:///Assets/Logo/douyin.png",
                LiveSite=new Douyin(),
            },
        };

    }
    public class Site
    {
        public LiveSite SiteType { get; set; }
        public string Name { get; set; }
        public ILiveSite LiveSite { get; set; }
        public string Logo { get; set; }
    }
}
