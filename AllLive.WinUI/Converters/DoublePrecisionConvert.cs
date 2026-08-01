using Windows.ApplicationModel;
using Microsoft.UI;
using AllLive.Core.Helper;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;

namespace AllLive.WinUI.Converters
{
    /// <summary>
    /// Double精度的转换器
    /// 部分控件的精度太高，导致无法绑定，所以需要转换
    /// </summary>
    public class DoublePrecisionConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return null;
            }
            int length = 1;
            if (parameter != null)
            {
                length = int.Parse(parameter.ToString());
            }
            var zero = "0.";
            for (int i = 0; i < length; i++)
            {
                zero += "0";
            }
            value = double.Parse(((double)value).ToString(zero));
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
