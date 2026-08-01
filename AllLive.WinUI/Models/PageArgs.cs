using Windows.ApplicationModel;
using Microsoft.UI;
using AllLive.Core.Helper;
﻿using AllLive.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllLive.WinUI.Models
{
    public class PageArgs
    {
        public ILiveSite Site { get; set; }
        public object Data { get; set; }
    }
}
