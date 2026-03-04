using System;
using System.Collections.Generic;
using System.Text;
using Tup.Tars;

namespace AllLive.Core.Models.Tars
{
    public class HuyaUserId : TarsStruct
    {
        public long lUid { get; set; } = 0;
        public string sGuid { get; set; } = "";
        public string sToken { get; set; } = "";
        public string sHuYaUA { get; set; } = "";
        public string sCookie { get; set; } = "";
        public int iTokenType { get; set; } = 0;
        public string sDeviceInfo { get; set; } = "";
        public string sQIMEI { get; set; } = "";

        public override void ReadFrom(TarsInputStream _is)
        {
            lUid = _is.Read(lUid, 0, false);
            sGuid = _is.Read(sGuid, 1, false);
            sToken = _is.Read(sToken, 2, false);
            sHuYaUA = _is.Read(sHuYaUA, 3, false);
            sCookie = _is.Read(sCookie, 4, false);
            iTokenType = _is.Read(iTokenType, 5, false);
            sDeviceInfo = _is.Read(sDeviceInfo, 6, false);
            sQIMEI = _is.Read(sQIMEI, 7, false);
        }

        public override void WriteTo(TarsOutputStream _os)
        {
            _os.Write(lUid, 0);
            _os.Write(sGuid, 1);
            _os.Write(sToken, 2);
            _os.Write(sHuYaUA, 3);
            _os.Write(sCookie, 4);
            _os.Write(iTokenType, 5);
            _os.Write(sDeviceInfo, 6);
            _os.Write(sQIMEI, 7);
        }
    }
}
