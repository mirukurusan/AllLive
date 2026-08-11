using System;
using Microsoft.UI.Xaml.Data;
using NSDanmaku.Model;

namespace AllLive.WinUI.Converters
{
    public partial class DanmakuStyleConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var style = (DanmakuBorderStyle)value;
            return (int)style;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
