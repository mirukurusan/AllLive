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
                subs.Add(new LiveSubCategory()
                {
                    Pic = $"https://huyaimg.msstatic.com/cdnimage/game/{item["gid"].ToString()}-MS.jpg",
                    ID = item["gid"].ToString(),
                    ParentID = id,
                    Name = item["gameFullName"].ToString(),
                });
            }
            return subs;
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

        public async Task<LiveRoomDetail> GetRoomDetail(object roomId)
        {
            var headers = new Dictionary<string, string>()
            {
                { "Accept", "*/*" },
                { "Origin", "https://www.huya.com" },
                { "Referer", "https://www.huya.com/" },
                { "User-Agent", kUserAgent },
            };
            var resultText = await HttpUtil.GetString($"https://mp.huya.com/cache.php?m=Live&do=profileRoom&roomid={roomId}&showSecret=1", headers);
            var result = JObject.Parse(resultText);

            if (result["status"]?.ToInt32() != 200 || result["data"]?["stream"] == null)
            {
                return new LiveRoomDetail() { RoomID = roomId.ToString(), Status = false };
            }

            var data = result["data"];
            var liveData = data["liveData"];
            var profileInfo = data["profileInfo"];
            var stream = data["stream"];

            long topSid = 0, subSid = 0, yySid = 0;
            var huyaLines = new List<HuyaLineModel>();
            var huyaBiterates = new List<HuyaBitRateModel>();

            var liveStatus = data["liveStatus"]?.ToString()?.ToUpper();
            var isLive = liveStatus == "ON"; // 只有 ON 才表示正在直播

            if (isLive)
            {
                yySid = profileInfo?["yyid"]?.ToInt64() ?? 0;

                // 获取有效线路
                var baseSteamInfoList = stream["baseSteamInfoList"] as JArray;
                if (baseSteamInfoList != null)
                {
                    var validLines = baseSteamInfoList.Where(line =>
                    {
                        int pc = line["iPCPriorityRate"]?.ToInt32() ?? -1;
                        int web = line["iWebPriorityRate"]?.ToInt32() ?? -1;
                        int mobile = line["iMobilePriorityRate"]?.ToInt32() ?? -1;
                        return pc > 0 || web > 0 || mobile > 0;
                    }).ToList();

                    foreach (var item in validLines)
                    {
                        var sFlvUrl = item["sFlvUrl"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(sFlvUrl))
                        {
                            var channelId = item["lChannelId"]?.ToInt64() ?? 0;
                            if (topSid == 0) topSid = channelId;
                            if (subSid == 0) subSid = item["lSubChannelId"]?.ToInt64() ?? 0;

                            huyaLines.Add(new HuyaLineModel()
                            {
                                Line = sFlvUrl,
                                LineType = HuyaLineType.FLV,
                                FlvAntiCode = item["sFlvAntiCode"]?.ToString() ?? "",
                                HlsAntiCode = item["sHlsAntiCode"]?.ToString() ?? "",
                                StreamName = item["sStreamName"]?.ToString() ?? "",
                                CdnType = item["sCdnType"]?.ToString() ?? "",
                                PresenterUid = channelId,
                            });
                        }
                    }
                }

                // 码率列表
                var bitRateInfoStr = liveData?["bitRateInfo"]?.ToString();
                JArray biterates = !string.IsNullOrEmpty(bitRateInfoStr)
                    ? JArray.Parse(bitRateInfoStr)
                    : (stream["flv"]?["rateArray"] as JArray ?? new JArray());
                foreach (var item in biterates)
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

            var title = liveData?["introduction"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(title)) title = liveData?["roomName"]?.ToString() ?? "";

            return new LiveRoomDetail()
            {
                Cover = liveData?["screenshot"]?.ToString() ?? "",
                Online = liveData?["userCount"]?.ToInt32() ?? 0,
                RoomID = roomId.ToString(),
                Title = title,
                UserName = profileInfo?["nick"]?.ToString() ?? "",
                UserAvatar = profileInfo?["avatar180"]?.ToString() ?? "",
                Introduction = liveData?["introduction"]?.ToString() ?? "",
                Notice = data["welcomeText"]?.ToString() ?? "",
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
                var headers = new Dictionary<string, string>()
                {
                    { "Accept", "*/*" },
                    { "Origin", "https://www.huya.com" },
                    { "Referer", "https://www.huya.com/" },
                    { "User-Agent", kUserAgent },
                };
                var resultText = await HttpUtil.GetString($"https://mp.huya.com/cache.php?m=Live&do=profileRoom&roomid={roomId}&showSecret=1", headers);
                var result = JObject.Parse(resultText);

                if (result["status"]?.ToInt32() != 200)
                {
                    System.Diagnostics.Trace.WriteLine($"Huya.GetLiveStatus: API returned status {result["status"]} for room {roomId}");
                    return LiveStatusType.Offline;
                }

                var liveStatus = result["data"]?["liveStatus"]?.ToString()?.ToUpper();
                System.Diagnostics.Trace.WriteLine($"Huya.GetLiveStatus: room {roomId} status = {liveStatus}");

                switch (liveStatus)
                {
                    case "ON":
                        return LiveStatusType.Live;
                    default:
                        return LiveStatusType.Offline;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Huya.GetLiveStatus error for room {roomId}: {ex.Message}");
                return LiveStatusType.Offline;
            }
        }

        public Task<List<LiveSuperChatMessage>> GetSuperChatMessages(object roomId)
        {
            return Task.FromResult(new List<LiveSuperChatMessage>());
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
