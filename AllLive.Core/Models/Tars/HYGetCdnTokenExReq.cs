using System;
using System.Collections.Generic;
using System.Text;
using Tup.Tars;

namespace AllLive.Core.Models.Tars
{
    public class HYGetCdnTokenExReq : TarsStruct
    {
        public string sFlvUrl { get; set; } = "";
        public string sStreamName { get; set; } = "";
        public int iLoopTime { get; set; } = 0;
        public HuyaUserId tId { get; set; } = new HuyaUserId();
        public int iAppId { get; set; } = 66;

        public override void ReadFrom(TarsInputStream _is)
        {
            sFlvUrl = _is.Read(sFlvUrl, 0, false);
            sStreamName = _is.Read(sStreamName, 1, false);
            iLoopTime = _is.Read(iLoopTime, 2, false);
            tId = _is.Read(tId, 3, false);
            iAppId = _is.Read(iAppId, 4, false);
        }

        public override void WriteTo(TarsOutputStream _os)
        {
            _os.Write(sFlvUrl, 0);
            _os.Write(sStreamName, 1);
            _os.Write(iLoopTime, 2);
            _os.Write(tId, 3);
            _os.Write(iAppId, 4);
        }
    }
}
