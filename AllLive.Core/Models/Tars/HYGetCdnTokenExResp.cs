using System;
using System.Collections.Generic;
using System.Text;
using Tup.Tars;

namespace AllLive.Core.Models.Tars
{
    public class HYGetCdnTokenExResp : TarsStruct
    {
        public string sFlvToken { get; set; } = "";
        public int iExpireTime { get; set; } = 0;

        public override void ReadFrom(TarsInputStream _is)
        {
            sFlvToken = _is.Read(sFlvToken, 0, false);
            iExpireTime = _is.Read(iExpireTime, 1, false);
        }

        public override void WriteTo(TarsOutputStream _os)
        {
            _os.Write(sFlvToken, 0);
            _os.Write(iExpireTime, 1);
        }
    }
}
