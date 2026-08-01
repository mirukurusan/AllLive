using Windows.ApplicationModel;
using Microsoft.UI;
using AllLive.Core.Helper;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace AllLive.WinUI.Converters
{
    public class CountTextConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value == null|| (int)value == 0)
            {
                return "";
            }
          
            return $"({value})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
