using System.Collections.ObjectModel;
using AllLive.WinUI.Helper;
using Newtonsoft.Json;

namespace AllLive.WinUI.ViewModels
{
    public class SettingVM
    {
        public SettingVM()
        {
            LoadShieldSetting();
        }
        public ObservableCollection<string> ShieldWords { get; set; }

        public void LoadShieldSetting()
        {
            ShieldWords =JsonConvert.DeserializeObject<ObservableCollection<string>>( SettingHelper.GetValue<string>(SettingHelper.LiveDanmaku.SHIELD_WORD,"[]"));
        }
    }
}
