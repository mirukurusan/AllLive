using AllLive.Core.Helper;
using AllLive.Core.Interface;
using AllLive.Core.Models;
using AllLive.Core.Models.Tars;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Timers;
using Tup.Tars;
/*
* 虎牙弹幕实现
* 参考项目：
* https://github.com/BacooTang/huya-danmu
* https://github.com/IsoaSFlus/danmaku
*/
namespace AllLive.Core.Danmaku
{
    public class HuyaDanmakuArgs
    {
        public HuyaDanmakuArgs(long ayyuid, long topSid, long subSid)
        {
            this.Ayyuid = ayyuid;
            this.SubSid = subSid;
            this.TopSid = topSid;
        }
        public long Ayyuid { get; set; }
        public long TopSid { get; set; }
        public long SubSid { get; set; }
    }
    public class HuyaDanmaku : ILiveDanmaku
    {
        private readonly Uri ServerUri;
        private readonly Timer HeartBeatTimer;
        private readonly ClientWebSocket WsClient;
        private readonly System.Threading.Thread ReceiveThread;
        private readonly byte[] HeartBeatData;

        private HuyaDanmakuArgs DanmakuArgs;

        public int HeartbeatTime => 60 * 1000;
        public event EventHandler<LiveMessage> NewMessageEvent;
        public event EventHandler<string> CloseEvent;

        public HuyaDanmaku()
        {
            ServerUri = new Uri("wss://cdnws.api.huya.com");
            WsClient = new ClientWebSocket();
            ReceiveThread = new System.Threading.Thread(ReceiveMessage);
            HeartBeatData = Convert.FromBase64String("ABQdAAwsNgBM");
            HeartBeatTimer = new Timer(HeartbeatTime);
            HeartBeatTimer.Elapsed += Timer_Elapsed;
        }

        private void ReceiveMessage()
        {
            var buffer = new byte[4096];
            while (WsClient.State == WebSocketState.Open)
            {
                try
                {
                    WsClient.ReceiveAsync(new ArraySegment<byte>(buffer), default).Wait();
                    var stream = new TarsInputStream(buffer);
                    var type = stream.Read(0, 0, false);
                    if (type == 7)
                    {
                        stream = new TarsInputStream(stream.Read(new byte[0], 1, false));
                        HYPushMessage wSPushMessage = new HYPushMessage();
                        wSPushMessage.ReadFrom(stream);
                        if (wSPushMessage.Uri == 1400)
                        {

                            HYMessage messageNotice = new HYMessage();
                            messageNotice.ReadFrom(new TarsInputStream(wSPushMessage.Msg));
                            var uname = messageNotice.UserInfo.NickName;
                            var content = messageNotice.Content;
                            var color = messageNotice.BulletFormat.FontColor;
                            NewMessageEvent?.Invoke(this, new LiveMessage()
                            {
                                Type = LiveMessageType.Chat,
                                Message = content,
                                UserName = uname,
                                Color = color <= 0 ? DanmakuColor.White : new DanmakuColor(color),
                            });

                        }
                        if (wSPushMessage.Uri == 8006)
                        {
                            long online = 0;
                            var s = new TarsInputStream(wSPushMessage.Msg);
                            online = s.Read(online, 0, false);
                            NewMessageEvent?.Invoke(this, new LiveMessage()
                            {
                                Type = LiveMessageType.Online,
                                Data = online,
                            });
                        }
                    }
                    else if (type == 22)
                    {
                        Debug.WriteLine($"收到消息:[Type:{type}]");
                        stream = new TarsInputStream(stream.Read(new byte[0], 1, false));
                        HYPushMessageV2 wSPushMessage = new HYPushMessageV2();
                        wSPushMessage.ReadFrom(stream);
                        foreach (var item in wSPushMessage.MsgItem)
                        {
                            if (item.Uri == 1400)
                            {
                                HYMessage messageNotice = new HYMessage();
                                messageNotice.ReadFrom(new TarsInputStream(item.Msg));
                                var uname = messageNotice.UserInfo.NickName;
                                var content = messageNotice.Content;
                                var color = messageNotice.BulletFormat.FontColor;
                                NewMessageEvent?.Invoke(this, new LiveMessage()
                                {
                                    Type = LiveMessageType.Chat,
                                    Message = content,
                                    UserName = uname,
                                    Color = color <= 0 ? DanmakuColor.White : new DanmakuColor(color),
                                });

                            }
                            if (item.Uri == 8006)
                            {
                                long online = 0;
                                var s = new TarsInputStream(item.Msg);
                                online = s.Read(online, 0, false);
                                NewMessageEvent?.Invoke(this, new LiveMessage()
                                {
                                    Type = LiveMessageType.Online,
                                    Data = online,
                                });
                            }
                        }

                    }
                }
                catch (Exception)
                {
                }
            }
            if (WsClient.State != WebSocketState.Open)
            {
                OnClose();
            }
        }

        private void OnClose()
        {
            CloseEvent?.Invoke(this, WsClient.State.ToString());
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Heartbeat();
        }

        public async void Heartbeat()
        {
            if (WsClient.State == WebSocketState.Open)
            {
                await WsClient.SendAsync(new ArraySegment<byte>(HeartBeatData), WebSocketMessageType.Binary, true, default);
            }
        }

        public async Task Start(object args)
        {
            DanmakuArgs = (HuyaDanmakuArgs)args;
            try
            {
                await WsClient.ConnectAsync(ServerUri, default);
                if (WsClient.State == WebSocketState.Open)
                {
                    //发送进房信息
                    await WsClient.SendAsync(JoinData(DanmakuArgs.Ayyuid, DanmakuArgs.TopSid, DanmakuArgs.SubSid), WebSocketMessageType.Binary, true, default);
                    HeartBeatTimer.Start();
                    ReceiveThread.Start();
                    //ReceiveMessage();
                }
                else
                {
                    OnClose();
                }
            }
            catch (Exception)
            {
                OnClose();
            }
        }

        public async Task Stop()
        {
            if (WsClient.State == WebSocketState.Connecting)
            {
                WsClient.Abort();
            }
            if (WsClient.State == WebSocketState.Open)
            {
                await WsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", default);
            }
            HeartBeatTimer.Stop();
        }

        private ArraySegment<byte> JoinData(long ayyuid, long tid, long sid)
        {
            var oos = new TarsOutputStream();
            oos.Write(ayyuid, 0);
            oos.Write(true, 1);
            oos.Write("", 2);
            oos.Write("", 3);
            oos.Write(tid, 4);
            oos.Write(sid, 5);
            oos.Write(0, 6);
            oos.Write(0, 7);

            var wscmd = new TarsOutputStream();
            wscmd.Write(1, 0);
            wscmd.Write(oos.toByteArray(), 1);
            return new ArraySegment<byte>(wscmd.toByteArray());
        }
    }
    
}
