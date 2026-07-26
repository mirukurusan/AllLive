using System;
using System.Collections.Generic;
using System.Text;
using Tup.Tars;

namespace AllLive.Core.Models.Tars
{
    /// <summary>
    /// 虎牙留言板用户信息
    /// </summary>
    public class HYMessageBoardUserInfo : TarsStruct
    {
        public string sAvatar { get; set; } = "";
        public string sNick { get; set; } = "";
        public long lUid { get; set; } = 0;
        public long lImid { get; set; } = 0;

        public override void ReadFrom(TarsInputStream _is)
        {
            sAvatar = _is.Read(sAvatar, 0, false);
            sNick = _is.Read(sNick, 1, false);
            lUid = _is.Read(lUid, 2, false);
            lImid = _is.Read(lImid, 3, false);
        }

        public override void WriteTo(TarsOutputStream _os)
        {
            _os.Write(sAvatar, 0);
            _os.Write(sNick, 1);
            _os.Write(lUid, 2);
            _os.Write(lImid, 3);
        }
    }

    /// <summary>
    /// 虎牙醒目留言信息
    /// </summary>
    public class HYGameEventMessageBoardInfo : TarsStruct
    {
        public string sContent { get; set; } = "";
        public int iCountDown { get; set; } = 0;
        public int iTotalSec { get; set; } = 0;
        public int iCost { get; set; } = 0;
        public int iCostPay { get; set; } = 0;
        public long lMessageId { get; set; } = 0;
        public HYMessageBoardUserInfo tMessageUser { get; set; } = new HYMessageBoardUserInfo();

        public override void ReadFrom(TarsInputStream _is)
        {
            sContent = _is.Read(sContent, 0, false);
            iCountDown = _is.Read(iCountDown, 1, false);
            iTotalSec = _is.Read(iTotalSec, 2, false);
            iCost = _is.Read(iCost, 3, false);
            iCostPay = _is.Read(iCostPay, 4, false);
            lMessageId = _is.Read(lMessageId, 5, false);
            tMessageUser = (HYMessageBoardUserInfo)_is.Read(tMessageUser, 6, false);
        }

        public override void WriteTo(TarsOutputStream _os)
        {
            _os.Write(sContent, 0);
            _os.Write(iCountDown, 1);
            _os.Write(iTotalSec, 2);
            _os.Write(iCost, 3);
            _os.Write(iCostPay, 4);
            _os.Write(lMessageId, 5);
            _os.Write(tMessageUser, 6);
        }
    }

    /// <summary>
    /// 虎牙留言板面板
    /// </summary>
    public class HYMessageBoardPanel : TarsStruct
    {
        public List<HYGameEventMessageBoardInfo> vGameEventMessageBoardInfo { get; set; } = new List<HYGameEventMessageBoardInfo>();

        public override void ReadFrom(TarsInputStream _is)
        {
            vGameEventMessageBoardInfo = _is.readArray(vGameEventMessageBoardInfo, 0, false) ?? new List<HYGameEventMessageBoardInfo>();
        }

        public override void WriteTo(TarsOutputStream _os)
        {
            _os.Write(vGameEventMessageBoardInfo, 0);
        }
    }

    /// <summary>
    /// 获取虎牙醒目留言请求
    /// </summary>
    public class HYGetGameEventMessageBoardReq : TarsStruct
    {
        public long lPid { get; set; } = 0;
        public HuyaUserId tId { get; set; } = new HuyaUserId();
        public int iMessageBoardScope { get; set; } = 0;
        public int iPageSize { get; set; } = 10;

        public override void ReadFrom(TarsInputStream _is)
        {
            lPid = _is.Read(lPid, 0, false);
            tId = (HuyaUserId)_is.Read(tId, 1, false);
            iMessageBoardScope = _is.Read(iMessageBoardScope, 2, false);
            iPageSize = _is.Read(iPageSize, 3, false);
        }

        public override void WriteTo(TarsOutputStream _os)
        {
            _os.Write(lPid, 0);
            _os.Write(tId, 1);
            _os.Write(iMessageBoardScope, 2);
            _os.Write(iPageSize, 3);
        }
    }

    /// <summary>
    /// 获取虎牙醒目留言响应
    /// </summary>
    public class HYGetGameEventMessageBoardRsp : TarsStruct
    {
        public HYMessageBoardPanel tMessageBoardPanel { get; set; } = new HYMessageBoardPanel();

        public override void ReadFrom(TarsInputStream _is)
        {
            tMessageBoardPanel = (HYMessageBoardPanel)_is.Read(tMessageBoardPanel, 0, false);
        }

        public override void WriteTo(TarsOutputStream _os)
        {
            _os.Write(tMessageBoardPanel, 0);
        }
    }
}
