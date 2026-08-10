using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Newtonsoft.Json;

namespace AllLive.WinUI.Helper
{
    public class SettingHelper
    {
        private static Dictionary<string, object> _fallbackSettings;
        private static string _fallbackPath;

        private static Dictionary<string, object> Settings
        {
            get
            {
                if (_fallbackSettings != null) return _fallbackSettings;

                try
                {
                    var container = ApplicationData.Current.LocalSettings;
                    // Test access
                    _ = container.Values;
                    return null; // Use LocalSettings directly
                }
                catch
                {
                    // Unpackaged mode — use JSON file fallback
                    _fallbackSettings = new Dictionary<string, object>();
                    _fallbackPath = Path.Combine(Utils.GetLocalFolderPath(), "settings.json");
                    if (File.Exists(_fallbackPath))
                    {
                        try { _fallbackSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(_fallbackPath)) ?? new Dictionary<string, object>(); } catch { }
                    }
                    return _fallbackSettings;
                }
            }
        }

        public static T GetValue<T>(string key, T _default)
        {
            var fallback = Settings;
            if (fallback != null)
            {
                if (fallback.ContainsKey(key))
                {
                    var val = fallback[key];
                    if (val is T typed) return typed;
                    try { return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(val)); } catch { return _default; }
                }
                return _default;
            }

            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(key))
            {
                var raw = localSettings.Values[key];
                if (raw is string json && typeof(T) != typeof(string))
                {
                    try { return JsonConvert.DeserializeObject<T>(json); } catch { return _default; }
                }
                if (raw is T typed2) return typed2;
                try { return (T)Convert.ChangeType(raw, typeof(T)); } catch { return _default; }
            }
            return _default;
        }

        public static void SetValue<T>(string key, T value)
        {
            var fallback = Settings;
            if (fallback != null)
            {
                fallback[key] = value;
                File.WriteAllText(_fallbackPath, JsonConvert.SerializeObject(fallback));
                return;
            }

            var localSettings = ApplicationData.Current.LocalSettings;
            if (value is string strVal)
                localSettings.Values[key] = strVal;
            else if (value is int || value is double || value is bool || value is long || value is float)
                localSettings.Values[key] = value;
            else
                localSettings.Values[key] = JsonConvert.SerializeObject(value);
        }
        /// <summary>
        /// 主题,0为默认，1为浅色，2为深色
        /// </summary>
        public const string THEME = "theme";
        /// <summary>
        /// 互动文字大小
        /// </summary>
        public const string MESSAGE_FONTSIZE = "MessageFontSize";
        /// <summary>
        /// 右侧详情宽度
        /// </summary>
        public const string RIGHT_DETAIL_WIDTH = "PlayerRightDetailWidth";

        /// <summary>
        /// 新窗口打开直播间
        /// </summary>
        public const string NEW_WINDOW_LIVEROOM = "newWindowLiveRoom";
        /// <summary>
        /// 鼠标功能键返回、关闭页面
        /// </summary>
        public const string MOUSE_BACK = "MouseBack";
        /// <summary>
        /// 关注加载线程数
        /// </summary>
        public const string CONCURRENCY_LEVEL = "ConcurrencyLevel";

        /// <summary>
        /// 视频解码
        /// </summary>
        public const string VIDEO_DECODER = "VideoDecoder";
        //public const string SORTWARE_DECODING = "sortwareDecoding";

        /// <summary>
        /// 默认清晰度
        /// </summary>
        public const string VIDEO_QUALITY = "VideoQuality";
        /// <summary>
        /// 数据网络默认清晰度
        /// </summary>
        public const string VIDEO_QUALITY_METERED = "VideoQualityMetered";

        /// <summary>
        /// 音量
        /// </summary>
        public const string PLAYER_VOLUME = "PlayerVolume";
        /// <summary>
        /// 亮度
        /// </summary>
        public const string PLAYER_BRIGHTNESS = "PlayeBrightness";

        /// <summary>
        /// 哔哩哔哩Cookie
        /// </summary>
        public const string BILI_COOKIE = "BiliCookie";

        /// <summary>
        /// 哔哩哔哩用户ID
        /// </summary>
        public const string BILI_USER_ID = "BiliUserId";

        /// <summary>
        /// NavigationView导航栏显示模式
        /// </summary>
        public const string PANE_DISPLAY_MODE = "PaneDisplayMode";

        /// <summary>
        /// 忽略哔哩哔哩登录提醒
        /// </summary>
        public const string IGNORE_BILI_LOGIN_TIP = "IgnoreBiliLoginTip";


        /// <summary>
        /// 抖音Cookie
        /// </summary>
        public const string DOUYIN_COOKIE = "DouyinCookie";
        public class LiveDanmaku
        {
            public const string TOP_MARGIN = "LiveTopMargin";
            /// <summary>
            /// 显示弹幕
            /// </summary>
            public const string SHOW = "LiveDanmuShowBool";
            /// <summary>
            /// 弹幕显示区域
            /// </summary>
            public const string AREA = "LiveDanmuArea";
            /// <summary>
            /// 弹幕缩放 double
            /// </summary>
            public const string FONT_ZOOM = "LiveDanmuFontZoom";
            /// <summary>
            /// 弹幕速度 int
            /// </summary>
            public const string SPEED = "LiveDanmuSpeed";
            /// <summary>
            /// 弹幕加粗 bool
            /// </summary>
            public const string BOLD = "LiveDanmuBold";
            /// <summary>
            /// 彩色弹幕 bool
            /// </summary>
            public const string COLOURFUL = "LiveDanmuColourful";
            /// <summary>
            /// 弹幕边框样式 int
            /// </summary>
            public const string BORDER_STYLE = "LiveDanmuStyle";

            /// <summary>
            /// 弹幕透明度 double，0-1
            /// </summary>
            public const string OPACITY = "LiveDanmuOpacity";
            /// <summary>
            /// 关键词屏蔽 ObservableCollection<string>
            /// </summary>
            public const string SHIELD_WORD = "LiveDanmuShieldWordString1";

            /// <summary>
            /// 直播弹幕清理
            /// </summary>
            public const string DANMU_CLEAN_COUNT = "LiveCleanCount";

            /// <summary>
            /// 保留醒目留言
            /// </summary>
            public const string KEEP_SUPER_CHAT = "KeepSuperChat";
        }

        /// <summary>
        /// 录制保存格式 0=ts, 1=mp4
        /// </summary>
        public const string RECORD_FORMAT = "RecordFormat";

        /// <summary>
        /// 默认开启直播录制
        /// </summary>
        public const string AUTO_START_RECORDING = "AutoStartRecording";
    }
}
