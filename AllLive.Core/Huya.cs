using AllLive.Core.Interface;
using AllLive.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AllLive.Core.Danmaku;
using AllLive.Core.Helper;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using AllLive.Core.Models.Tars;
using System.Security.Cryptography;

namespace AllLive.Core
{
    public class Huya : ILiveSite
    {
        public string Name => "虎牙直播";
        public ILiveDanmaku GetDanmaku() => new HuyaDanmaku();

        private const string kUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        private const string kMobileUserAgent = "Mozilla/5.0 (Linux; Android 11; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.91 Mobile Safari/537.36 Edg/117.0.0.0";
        private const string HYSDK_UA = "HYSDK(Windows, 30000002)_APP(pc_exe&7060000&official)_SDK(trans&2.32.3.5646)";

        private static readonly Dictionary<string, string> requestHeaders = new Dictionary<string, string>()
        {
            { "Origin", "https://www.huya.com" },
            { "Referer", "https://www.huya.com" },
            { "User-Agent", kUserAgent },
        };

        private TupHttpHelper _tupClient;
        private TupHttpHelper tupClient
        {
            get
            {
                if (_tupClient == null)
                {
                    _tupClient = new TupHttpHelper("http://wup.huya.com", "liveui", HYSDK_UA, new Dictionary<string, string>()
                    {
                        { "Origin", "https://m.huya.com/" },
                        { "Referer", "https://m.huya.com/" },
                    });
                }
                return _tupClient;
            }
        }

        private TupHttpHelper _messageBoardClient;
        private TupHttpHelper messageBoardClient
        {
            get
            {
                if (_messageBoardClient == null)
                {
                    _messageBoardClient = new TupHttpHelper("http://wup.huya.com", "wupui", HYSDK_UA, new Dictionary<string, string>()
                    {
                        { "Origin", "https://m.huya.com/" },
                        { "Referer", "https://m.huya.com/" },
                    });
                }
                return _messageBoardClient;
            }
        }

        private DateTime? _lastHeadlineEmptyLogAt;

        public async Task<List<LiveCategory>> GetCategores()
        {
            List<LiveCategory> categories = new List<LiveCategory>() {
                new LiveCategory() { ID="1", Name="网游" },
                new LiveCategory() { ID="2", Name="单机" },
                new LiveCategory() { ID="8", Name="娱乐" },
                new LiveCategory() { ID="3", Name="手游" },
            };
            foreach (var item in categories)
            {
                item.Children = await GetSubCategories(item.ID);
            }
            return categories;
        }

        private async Task<List<LiveSubCategory>> GetSubCategories(string id)
        {
            List<LiveSubCategory> subs = new List<LiveSubCategory>();
            var result = await HttpUtil.GetString($"https://live.cdn.huya.com/liveconfig/game/bussLive?bussType={id}");
            var obj = JObject.Parse(result);
            foreach (var item in obj["data"])
            {
                var gid = ResolveGid(item["gid"]);
                subs.Add(new LiveSubCategory()
                {
                    Pic = $"https://huyaimg.msstatic.com/cdnimage/game/{gid}-MS.jpg",
                    ID = gid,
                    ParentID = id,
                    Name = item["gameFullName"].ToString(),
                });
            }
            return subs;
        }

        /// <summary>
        /// 解析虎牙 gid 字段，兼容多种数据类型
        /// </summary>
        private static string ResolveGid(JToken gidToken)
        {
            if (gidToken == null) return "";

            // Map 类型: {"value": "1,2,3"}
            if (gidToken is JObject gidObj)
            {
                var value = gidObj["value"]?.ToString();
                return value?.Split(',')[0] ?? "";
            }
            // 浮点数类型
            if (gidToken.Type == JTokenType.Float)
            {
                return ((int)(double)gidToken).ToString();
            }
            // 整数或字符串
            return gidToken.ToString();
        }

        public async Task<LiveCategoryResult> GetCategoryRooms(LiveSubCategory category, int page = 1)
        {
            LiveCategoryResult categoryResult = new LiveCategoryResult() { Rooms = new List<LiveRoomItem>() };
            var result = await HttpUtil.GetString($"https://www.huya.com/cache.php?m=LiveList&do=getLiveListByPage&tagAll=0&gameId={category.ID}&page={page}");
            var obj = JObject.Parse(result);
            foreach (var item in obj["data"]["datas"])
            {
                var cover = item["screenshot"].ToString();
                if (!cover.Contains("?")) cover += "?x-oss-process=style/w338_h190&";
                var title = item["introduction"]?.ToString();
                if (string.IsNullOrEmpty(title)) title = item["roomName"]?.ToString() ?? "";
                categoryResult.Rooms.Add(new LiveRoomItem()
                {
                    Cover = cover,
                    Online = item["totalCount"].ToInt32(),
                    RoomID = item["profileRoom"].ToString(),
                    Title = title,
                    UserName = item["nick"].ToString(),
                });
            }
            categoryResult.HasMore = obj["data"]["page"].ToInt32() < obj["data"]["totalPage"].ToInt32();
            return categoryResult;
        }

        public async Task<LiveCategoryResult> GetRecommendRooms(int page = 1)
        {
            LiveCategoryResult categoryResult = new LiveCategoryResult() { Rooms = new List<LiveRoomItem>() };
            var result = await HttpUtil.GetString($"https://www.huya.com/cache.php?m=LiveList&do=getLiveListByPage&tagAll=0&page={page}");
            var obj = JObject.Parse(result);
            foreach (var item in obj["data"]["datas"])
            {
                var cover = item["screenshot"].ToString();
                if (!cover.Contains("?")) cover += "?x-oss-process=style/w338_h190&";
                var title = item["introduction"]?.ToString();
                if (string.IsNullOrEmpty(title)) title = item["roomName"]?.ToString() ?? "";
                categoryResult.Rooms.Add(new LiveRoomItem()
                {
                    Cover = cover,
                    Online = item["totalCount"].ToInt32(),
                    RoomID = item["profileRoom"].ToString(),
                    Title = title,
                    UserName = item["nick"].ToString(),
                });
            }
            categoryResult.HasMore = obj["data"]["page"].ToInt32() < obj["data"]["totalPage"].ToInt32();
            return categoryResult;
        }

        /// <summary>
        /// 通过抓取虎牙移动端页面获取房间信息 (解析 window.HNF_GLOBAL_INIT)
        /// </summary>
        private async Task<JObject> GetRoomInfo(string roomId)
        {
            var headers = new Dictionary<string, string>()
            {
                { "User-Agent", kMobileUserAgent },
            };

            var html = await HttpUtil.GetString($"https://m.huya.com/{roomId}", headers);

            // 提取 window.HNF_GLOBAL_INIT 中的 JSON
            var match = Regex.Match(html, @"window\.HNF_GLOBAL_INIT\s*=\s*\{[\s\S]*?\}[\s\S]*?</script>", RegexOptions.None);
            if (!match.Success)
            {
                System.Diagnostics.Trace.WriteLine($"Huya.GetRoomInfo: HNF_GLOBAL_INIT not found for room {roomId}");
                return null;
            }

            var jsonText = match.Value;
            jsonText = Regex.Replace(jsonText, @"window\.HNF_GLOBAL_INIT\s*=\s*", "");
            jsonText = Regex.Replace(jsonText, @"</script>", "");
            // 替换函数定义为空字符串
            jsonText = Regex.Replace(jsonText, @"function\s*\([^)]*\)\s*\{[\s\S]*?\}", "\"\"");

            JObject jsonObj;
            try
            {
                jsonObj = JObject.Parse(jsonText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Huya.GetRoomInfo: JSON parse error for room {roomId}: {ex.Message}");
                return null;
            }

            // 提取 topSid / subSid (频道ID)
            long topSid = 0, subSid = 0;
            var topMatch = Regex.Match(html, @"lChannelId"":\s*(\d+)");
            if (topMatch.Success) long.TryParse(topMatch.Groups[1].Value, out topSid);

            var subMatch = Regex.Match(html, @"lSubChannelId"":\s*(\d+)");
            if (subMatch.Success) long.TryParse(subMatch.Groups[1].Value, out subSid);

            // 回退：在 JSON 中递归搜索
            if (topSid <= 0) topSid = FirstPositiveIntByKeys(jsonObj, new HashSet<string> { "lchannelid", "channelid" });
            if (subSid <= 0) subSid = FirstPositiveIntByKeys(jsonObj, new HashSet<string> { "lsubchannelid", "subchannelid" });

            jsonObj["_topSid"] = topSid;
            jsonObj["_subSid"] = subSid;

            return jsonObj;
        }

        /// <summary>
        /// 将值转换为正整数，非正数返回 0
        /// </summary>
        private static long AsPositiveInt64(object value)
        {
            if (value is long l) return l > 0 ? l : 0;
            if (value is int i) return i > 0 ? i : 0;
            var str = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(str)) return 0;
            return long.TryParse(str, out var result) && result > 0 ? result : 0;
        }

        /// <summary>
        /// 在 JToken 树中递归搜索匹配 key 的第一个正整数
        /// </summary>
        private static long FirstPositiveIntByKeys(JToken source, HashSet<string> keys, int depth = 0)
        {
            if (source == null || depth > 8) return 0;

            if (source is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    var key = prop.Name?.ToLower();
                    if (key != null && keys.Contains(key))
                    {
                        var val = AsPositiveInt64(prop.Value);
                        if (val > 0) return val;
                    }
                }
                foreach (var prop in obj.Properties())
                {
                    var result = FirstPositiveIntByKeys(prop.Value, keys, depth + 1);
                    if (result > 0) return result;
                }
            }
            else if (source is JArray arr)
            {
                foreach (var item in arr)
                {
                    var result = FirstPositiveIntByKeys(item, keys, depth + 1);
                    if (result > 0) return result;
                }
            }
            return 0;
        }

        public async Task<LiveRoomDetail> GetRoomDetail(object roomId)
        {
            var roomInfo = await GetRoomInfo(roomId.ToString());
            if (roomInfo == null)
            {
                return new LiveRoomDetail() { RoomID = roomId.ToString(), Status = false };
            }

            var tLiveInfo = roomInfo["roomInfo"]?["tLiveInfo"];
            var tProfileInfo = roomInfo["roomInfo"]?["tProfileInfo"];
            var topSid = roomInfo["_topSid"]?.ToInt64() ?? 0;
            var subSid = roomInfo["_subSid"]?.ToInt64() ?? 0;

            long yySid = 0;
            var huyaLines = new List<HuyaLineModel>();
            var huyaBiterates = new List<HuyaBitRateModel>();

            var liveStatus = roomInfo["roomInfo"]?["eLiveStatus"]?.ToInt32();
            var isLive = liveStatus == 2; // eLiveStatus == 2 表示正在直播

            if (isLive && tLiveInfo != null)
            {
                yySid = tLiveInfo["lYyid"]?.ToInt64() ?? 0;

                // 读取可用线路
                var streamInfoList = tLiveInfo["tLiveStreamInfo"]?["vStreamInfo"]?["value"] as JArray;
                if (streamInfoList != null)
                {
                    foreach (var item in streamInfoList)
                    {
                        var sFlvUrl = item["sFlvUrl"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(sFlvUrl))
                        {
                            // presenterUid: 优先使用 topSid，否则 subSid
                            var presenterUid = topSid > 0 ? topSid : subSid;

                            huyaLines.Add(new HuyaLineModel()
                            {
                                Line = sFlvUrl,
                                LineType = HuyaLineType.FLV,
                                FlvAntiCode = item["sFlvAntiCode"]?.ToString() ?? "",
                                HlsAntiCode = item["sHlsAntiCode"]?.ToString() ?? "",
                                StreamName = item["sStreamName"]?.ToString() ?? "",
                                CdnType = item["sCdnType"]?.ToString() ?? "",
                                PresenterUid = presenterUid,
                            });
                        }
                    }
                }

                // 读取清晰度
                var bitRateList = tLiveInfo["tLiveStreamInfo"]?["vBitRateInfo"]?["value"] as JArray;
                if (bitRateList != null)
                {
                    foreach (var item in bitRateList)
                    {
                        var name = item["sDisplayName"]?.ToString() ?? "";
                        if (name.Contains("HDR")) continue;
                        if (!huyaBiterates.Any(x => x.Name == name))
                        {
                            huyaBiterates.Add(new HuyaBitRateModel()
                            {
                                BitRate = item["iBitRate"]?.ToInt32() ?? 0,
                                Name = name,
                            });
                        }
                    }
                }
            }

            var title = tLiveInfo?["sIntroduction"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(title)) title = tLiveInfo?["sRoomName"]?.ToString() ?? "";

            var lProfileRoom = tLiveInfo?["lProfileRoom"]?.ToInt64() ?? 0;

            return new LiveRoomDetail()
            {
                Cover = tLiveInfo?["sScreenshot"]?.ToString() ?? "",
                Online = tLiveInfo?["lTotalCount"]?.ToInt32() ?? 0,
                RoomID = lProfileRoom > 0 ? lProfileRoom.ToString() : roomId.ToString(),
                Title = title,
                UserName = tProfileInfo?["sNick"]?.ToString() ?? "",
                UserAvatar = tProfileInfo?["sAvatar180"]?.ToString() ?? "",
                Introduction = tLiveInfo?["sIntroduction"]?.ToString() ?? "",
                Notice = roomInfo["welcomeText"]?.ToString() ?? "",
                Status = isLive,
                Data = new HuyaUrlDataModel()
                {
                    Url = "",
                    Lines = huyaLines,
                    BitRates = huyaBiterates,
                },
                DanmakuData = new HuyaDanmakuArgs(yySid, topSid, subSid),
                Url = "https://www.huya.com/" + roomId
            };
        }

        public async Task<LiveSearchResult> Search(string keyword, int page = 1)
        {
            LiveSearchResult searchResult = new LiveSearchResult() { Rooms = new List<LiveRoomItem>() };
            var headers = new Dictionary<string, string>()
            {
                { "user-agent", kUserAgent },
                { "referer", "https://www.huya.com/" }
            };
            var result = await HttpUtil.GetUtf8String($"https://search.cdn.huya.com/?m=Search&do=getSearchContent&q={Uri.EscapeDataString(keyword)}&uid=0&v=4&typ=-5&livestate=0&rows=20&start={(page - 1) * 20}", headers);
            var obj = JObject.Parse(result);
            foreach (var item in obj["response"]["3"]["docs"])
            {
                var cover = item["game_screenshot"].ToString();
                if (!cover.Contains("?")) cover += "?x-oss-process=style/w338_h190&";
                searchResult.Rooms.Add(new LiveRoomItem()
                {
                    Cover = cover,
                    Online = item["game_total_count"].ToInt32(),
                    RoomID = item["room_id"].ToString(),
                    Title = item["game_roomName"].ToString(),
                    UserName = item["game_nick"].ToString(),
                });
            }
            searchResult.HasMore = obj["response"]["3"]["numFound"].ToInt32() > (page * 20);
            return searchResult;
        }

        public Task<List<LivePlayQuality>> GetPlayQuality(LiveRoomDetail roomDetail)
        {
            List<LivePlayQuality> qualities = new List<LivePlayQuality>();
            var urlData = roomDetail.Data as HuyaUrlDataModel;
            if (urlData == null) return Task.FromResult(qualities);

            if (urlData.BitRates == null || urlData.BitRates.Count == 0)
            {
                urlData.BitRates = new List<HuyaBitRateModel>()
                {
                    new HuyaBitRateModel() { Name = "原画", BitRate = 0 },
                    new HuyaBitRateModel() { Name = "高清", BitRate = 2000 },
                };
            }

            foreach (var item in urlData.BitRates)
            {
                qualities.Add(new LivePlayQuality()
                {
                    Data = new HuyaQualityData() { BitRate = item.BitRate, Lines = urlData.Lines ?? new List<HuyaLineModel>() },
                    Quality = item.Name,
                });
            }
            return Task.FromResult(qualities);
        }

        public async Task<List<string>> GetPlayUrls(LiveRoomDetail roomDetail, LivePlayQuality qn)
        {
            var data = qn.Data as HuyaQualityData;
            var urls = new List<string>();
            if (data?.Lines == null) return urls;

            foreach (var line in data.Lines)
            {
                urls.Add(await GetPlayUrl(line, data.BitRate));
            }
            return urls;
        }

        private async Task<string> GetPlayUrl(HuyaLineModel line, int bitRate)
        {
            try
            {
                var antiCode = await GetCdnTokenInfoEx(line.StreamName);
                antiCode = BuildAntiCode(line.StreamName, line.PresenterUid, antiCode);
                var baseUrl = line.Line;
                if (!baseUrl.StartsWith("http")) baseUrl = "https://" + baseUrl;
                var url = $"{baseUrl}/{line.StreamName}.flv?{antiCode}&codec=264";
                if (bitRate > 0) url += $"&ratio={bitRate}";
                return url;
            }
            catch
            {
                // fallback: 使用原始的 antiCode
                var fallbackUrl = line.Line;
                if (!fallbackUrl.StartsWith("http")) fallbackUrl = "https://" + fallbackUrl;
                fallbackUrl = $"{fallbackUrl}/{line.StreamName}.flv?{line.FlvAntiCode}&codec=264";
                if (bitRate > 0) fallbackUrl += $"&ratio={bitRate}";
                return fallbackUrl;
            }
        }

        private async Task<string> GetCdnTokenInfoEx(string stream)
        {
            var tid = new HuyaUserId();
            tid.sHuYaUA = "pc_exe&7060000&official";
            var tReq = new HYGetCdnTokenExReq();
            tReq.tId = tid;
            tReq.sStreamName = stream;
            var resp = await tupClient.GetAsync(tReq, "getCdnTokenInfoEx", new HYGetCdnTokenExResp());
            return resp.sFlvToken;
        }

        private string BuildAntiCode(string stream, long presenterUid, string antiCode)
        {
            var query = HttpUtility.ParseQueryString(antiCode);
            if (string.IsNullOrEmpty(query["fm"]))
            {
                return antiCode;
            }

            var ctype = query["ctype"] ?? "huya_pc_exe";
            int platformId = 0;
            int.TryParse(query["t"] ?? "0", out platformId);

            bool isWap = platformId == 103;
            var clacStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var seqId = presenterUid + clacStartTime;
            var secretHash = Md5Hash($"{seqId}|{ctype}|{platformId}");

            var convertUid = Rotl64(presenterUid);
            var calcUid = isWap ? presenterUid : convertUid;
            var fm = Uri.UnescapeDataString(query["fm"]);
            var secretPrefix = Encoding.UTF8.GetString(Convert.FromBase64String(fm)).Split('_')[0];
            var wsTime = query["wsTime"];
            var secretStr = $"{secretPrefix}_{calcUid}_{stream}_{secretHash}_{wsTime}";

            var wsSecret = Md5Hash(secretStr);

            var rnd = new Random();
            var ct = (long)((long.Parse(wsTime, System.Globalization.NumberStyles.HexNumber) + rnd.NextDouble()) * 1000);
            var uuid = ((long)((ct % 1e10 + rnd.NextDouble()) * 1e3 % 0xffffffff)).ToString();

            var sb = new StringBuilder();
            sb.Append($"wsSecret={wsSecret}");
            sb.Append($"&wsTime={wsTime}");
            sb.Append($"&seqid={seqId}");
            sb.Append($"&ctype={ctype}");
            sb.Append($"&ver=1");
            sb.Append($"&fs={query["fs"]}");
            sb.Append($"&fm={Uri.EscapeDataString(query["fm"])}");
            sb.Append($"&t={platformId}");
            if (isWap)
            {
                sb.Append($"&uid={presenterUid}");
                sb.Append($"&uuid={uuid}");
            }
            else
            {
                sb.Append($"&u={convertUid}");
            }

            return sb.ToString();
        }

        private static long Rotl64(long t)
        {
            var low = t & 0xFFFFFFFF;
            var rotatedLow = ((low << 8) | ((low >> 24) & 0xFF)) & 0xFFFFFFFF;
            var high = t & ~0xFFFFFFFF;
            return high | rotatedLow;
        }

        private static string Md5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public async Task<LiveStatusType> GetLiveStatus(object roomId)
        {
            try
            {
                var roomInfo = await GetRoomInfo(roomId.ToString());
                if (roomInfo == null)
                {
                    return LiveStatusType.Offline;
                }

                var liveStatus = roomInfo["roomInfo"]?["eLiveStatus"]?.ToInt32();
                System.Diagnostics.Trace.WriteLine($"Huya.GetLiveStatus: room {roomId} eLiveStatus = {liveStatus}");

                return liveStatus == 2 ? LiveStatusType.Live : LiveStatusType.Offline;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Huya.GetLiveStatus error for room {roomId}: {ex.Message}");
                return LiveStatusType.Offline;
            }
        }

        /// <summary>
        /// 虎牙弹幕连接不推送SC，需要轮询拉取
        /// </summary>
        public bool NeedPollSuperChat => true;

        public async Task<List<LiveSuperChatMessage>> GetSuperChatMessages(object roomId, LiveRoomDetail detail = null)
        {
            try
            {
                var roomIdStr = roomId?.ToString();
                if (string.IsNullOrWhiteSpace(roomIdStr) || roomIdStr == "0")
                {
                    return new List<LiveSuperChatMessage>();
                }

                var pidCandidates = new List<long>();
                long topSid = 0, subSid = 0;

                if (detail?.DanmakuData is HuyaDanmakuArgs danmakuArgs)
                {
                    topSid = danmakuArgs.TopSid;
                    subSid = danmakuArgs.SubSid;
                }
                if (detail?.Data is HuyaUrlDataModel urlData && urlData.Lines != null)
                {
                    foreach (var line in urlData.Lines)
                    {
                        if (line.PresenterUid > 0) pidCandidates.Add(line.PresenterUid);
                    }
                }
                if (topSid > 0) pidCandidates.Add(topSid);
                if (subSid > 0 && subSid != topSid) pidCandidates.Add(subSid);

                long.TryParse(roomIdStr, out var profileRoomId);
                if (profileRoomId > 0) pidCandidates.Add(profileRoomId);

                if (pidCandidates.Count == 0 ||
                    (pidCandidates.Count == 1 && pidCandidates.Contains(profileRoomId)))
                {
                    JObject roomInfo;
                    try
                    {
                        roomInfo = await GetRoomInfo(roomIdStr);
                    }
                    catch
                    {
                        roomInfo = null;
                    }

                    if (roomInfo == null)
                    {
                        LogHeadlineEmpty(roomIdStr, pidCandidates, "room info unavailable");
                        return new List<LiveSuperChatMessage>();
                    }

                    var eLiveStatus = roomInfo["roomInfo"]?["eLiveStatus"]?.ToObject<int?>();
                    if (eLiveStatus != null && eLiveStatus != 2)
                    {
                        return new List<LiveSuperChatMessage>();
                    }

                    if (topSid <= 0) topSid = roomInfo["_topSid"]?.ToInt64() ?? 0;
                    if (subSid <= 0) subSid = roomInfo["_subSid"]?.ToInt64() ?? 0;
                    if (topSid > 0) pidCandidates.Add(topSid);
                    if (subSid > 0 && subSid != topSid) pidCandidates.Add(subSid);
                }

                pidCandidates = pidCandidates.Distinct().Where(p => p > 0).ToList();

                if (pidCandidates.Count == 0)
                {
                    LogHeadlineEmpty(roomIdStr, pidCandidates, "no pid candidate");
                    return new List<LiveSuperChatMessage>();
                }

                foreach (var pid in pidCandidates)
                {
                    try
                    {
                        var messages = await FetchHeadLineMessages(pid);
                        if (messages.Count > 0)
                        {
                            return messages;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine($"Huya headline fetch failed for pid={pid}: {ex.Message}");
                    }
                }

                LogHeadlineEmpty(roomIdStr, pidCandidates, "empty response");
                return new List<LiveSuperChatMessage>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Huya.GetSuperChatMessages error for room {roomId}: {ex.Message}");
                return new List<LiveSuperChatMessage>();
            }
        }

        private void LogHeadlineEmpty(string roomId, IEnumerable<long> pidCandidates, string reason)
        {
            var now = DateTime.Now;
            var last = _lastHeadlineEmptyLogAt;
            if (last != null && now.Subtract(last.Value).TotalMinutes < 1)
            {
                return;
            }
            _lastHeadlineEmptyLogAt = now;
            System.Diagnostics.Trace.WriteLine($"Huya headline {reason}, roomId={roomId}, pidCandidates={string.Join(",", pidCandidates)}");
        }

        /// <summary>
        /// 通过 Tars 协议获取虎牙直播间醒目留言
        /// </summary>
        private async Task<List<LiveSuperChatMessage>> FetchHeadLineMessages(long pid)
        {
            var userId = new HuyaUserId();
            userId.sHuYaUA = HYSDK_UA;
            var req = new HYGetGameEventMessageBoardReq();
            req.lPid = pid;
            req.tId = userId;
            req.iMessageBoardScope = 0;
            req.iPageSize = 10;

            var rsp = await messageBoardClient.GetAsync(req, "getHeadLineMessageBoard", new HYGetGameEventMessageBoardRsp());

            var messages = new List<LiveSuperChatMessage>();
            var now = DateTime.Now;

            foreach (var item in rsp.tMessageBoardPanel.vGameEventMessageBoardInfo)
            {
                var content = item.sContent?.Trim() ?? "";
                if (string.IsNullOrEmpty(content)) continue;

                var remainingSeconds = item.iCountDown > 0 ? item.iCountDown : item.iTotalSec;
                if (remainingSeconds <= 0) continue;

                var totalSeconds = item.iTotalSec > 0 ? item.iTotalSec : remainingSeconds;
                var price = item.iCost > 0
                    ? item.iCost
                    : item.iCostPay > 0
                        ? Math.Max(1, (int)Math.Round(item.iCostPay / 100.0))
                        : 0;

                var endTime = now.AddSeconds(Math.Max(1, remainingSeconds));
                var startTime = endTime.AddSeconds(-Math.Max(1, totalSeconds));

                messages.Add(new LiveSuperChatMessage()
                {
                    Id = item.lMessageId > 0 ? item.lMessageId.ToString() : null,
                    UserName = item.tMessageUser?.sNick?.Trim() ?? "",
                    Face = item.tMessageUser?.sAvatar ?? "",
                    Message = content,
                    Price = price,
                    StartTime = startTime,
                    EndTime = endTime,
                    BackgroundColor = "#FED7AA",
                    BackgroundBottomColor = "#F97316",
                });
            }

            return messages;
        }
    }

    public class HuyaUrlDataModel
    {
        public string Url { get; set; }
        public List<HuyaLineModel> Lines { get; set; }
        public List<HuyaBitRateModel> BitRates { get; set; }
    }

    public enum HuyaLineType { FLV = 0, HLS = 1 }

    public class HuyaLineModel
    {
        public string Line { get; set; }
        public string FlvAntiCode { get; set; }
        public string StreamName { get; set; }
        public string HlsAntiCode { get; set; }
        public string CdnType { get; set; }
        public HuyaLineType LineType { get; set; }
        public long PresenterUid { get; set; }
    }

    public class HuyaBitRateModel
    {
        public string Name { get; set; }
        public int BitRate { get; set; }
    }

    public class HuyaQualityData
    {
        public int BitRate { get; set; }
        public List<HuyaLineModel> Lines { get; set; }
    }
}
