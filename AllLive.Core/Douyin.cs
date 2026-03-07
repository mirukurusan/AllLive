using AllLive.Core.Danmaku;
using AllLive.Core.Helper;
using AllLive.Core.Interface;
using AllLive.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Security.Cryptography;

namespace AllLive.Core
{
    public class Douyin : ILiveSite
    {
        public string Name => "抖音直播";
        public ILiveDanmaku GetDanmaku() => new DouyinDanmaku();

        // 使用 QQBrowser User-Agent（参考 DouyinLiveRecorder / dart_simple_live）
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.5845.97 Safari/537.36 Core/1.116.567.400 QQBrowser/19.7.6764.400";
        private const string REFERER = "https://live.douyin.com";
        private const string AUTHORITY = "live.douyin.com";
        
        // 默认Cookie，只需要ttwid即可获取所有画质（参考pure_live项目）
        private const string DEFAULT_COOKIE = "ttwid=1%7CB1qls3GdnZhUov9o2NxOMxxYS2ff6OSvEWbv0ytbES4%7C1680522049%7C280d802d6d478e3e78d0c807f7c487e7ffec0ae4e5fdd6a0fe74c3c6af149511";

        Dictionary<string, string> headers = new Dictionary<string, string>
        {
            { "User-Agent", USER_AGENT },
            { "Referer", REFERER },
            { "Authority", AUTHORITY }
        };

        private async Task<Dictionary<string, string>> GetRequestHeaders(bool forceRefresh = false)
        {
            // 如果已有Cookie且不需要强制刷新，直接返回
            if (!forceRefresh && (headers.ContainsKey("Cookie") || headers.ContainsKey("cookie")))
            {
                return headers;
            }
            
            // 直接使用默认Cookie（只需要ttwid即可获取所有画质，参考dart_simple_live）
            headers["Cookie"] = DEFAULT_COOKIE;
            return headers;
        }

        public async Task<List<LiveCategory>> GetCategores()
        {
            List<LiveCategory> categories = new List<LiveCategory>();
            var resp = await HttpUtil.GetString("https://live.douyin.com/", await GetRequestHeaders());

            Regex regex = new Regex("\\{\\\\\"pathname\\\\\":\\\\\"\\/\\\\\",\\\\\"categoryData.*?\\]\\\\n", RegexOptions.Singleline);
            Match match = regex.Match(resp);
            string renderData = match.Success ? match.Groups[0].Value : "";
            if (string.IsNullOrEmpty(renderData))
            {
                throw new Exception("Unable to get category data");
            }
            renderData = renderData.Trim().Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("]\\n", "");
            // Parse JSON data
            var renderDataJson = JObject.Parse(renderData);
            foreach (var item in renderDataJson["categoryData"])
            {
                List<LiveSubCategory> subs = new List<LiveSubCategory>();
                var id = $"{item["partition"]["id_str"]},{item["partition"]["type"]}";
                foreach (var subItem in item["sub_partition"])
                {
                    var subCategory = new LiveSubCategory()
                    {
                        ID = $"{subItem["partition"]["id_str"]},{subItem["partition"]["type"]}",
                        Name = subItem["partition"]["title"].ToString(),
                        ParentID = id,
                        Pic = "",
                    };
                    subs.Add(subCategory);
                }
                var category = new LiveCategory()
                {
                    Children = subs,
                    ID = id,
                    Name = item["partition"]["title"].ToString(),
                };
                subs.Insert(0, new LiveSubCategory() { ID = category.ID, Name = category.Name, ParentID = category.ID, Pic = "" });
                categories.Add(category);
            }
            return categories;
        }

        public async Task<LiveCategoryResult> GetCategoryRooms(LiveSubCategory category, int page = 1)
        {
            var ids = category.ID.Split(',');
            var partitionId = ids[0];
            var partitionType = ids[1];
            var reqParams = new Dictionary<string, string> {
                    {"aid","6383"},
                    {"app_name","douyin_web" },
                    {"live_id", "1"},
                    {"device_platform","web" },
                    { "language", "zh-CN"},
                    { "enter_from", "link_share"},
                    { "cookie_enabled", "true"},
                    { "screen_width", "1980"},
                    { "screen_height", "1080"},
                    { "browser_language", "zh-CN"},
                    { "browser_platform", "Win32"},
                    { "browser_name", "Edge"},
                    { "browser_version", "125.0.0.0"},
                    {"browser_online", "true"},
                    { "count","15" },
                    { "offset", ((page - 1) * 15).ToString()},
                    {"partition",partitionId},
                    {"partition_type",partitionType},
                    {"req_from","2" }
                };
            var url = $"https://live.douyin.com/webcast/web/partition/detail/room/v2/?{Utils.BuildQueryString(reqParams)}";

            var requestUrl = await GetABougs(url);
            var resp = await HttpUtil.GetString(requestUrl,
                headers: await GetRequestHeaders()
            );
            Trace.WriteLine($"Douyin.GetCategoryRooms url: {requestUrl}");
            if (string.IsNullOrWhiteSpace(resp) || !resp.TrimStart().StartsWith("{"))
            {
                Trace.WriteLine($"Douyin.GetCategoryRooms invalid response: {resp}");
                return new LiveCategoryResult()
                {
                    HasMore = false,
                    Rooms = new List<LiveRoomItem>()
                };
            }
            var json = JObject.Parse(resp);
            var hasMore = (json["data"]["data"] as JArray).Count >= 15;
            var items = new List<LiveRoomItem>();
            foreach (var item in json["data"]["data"])
            {
                var roomItem = new LiveRoomItem()
                {
                    RoomID = item["web_rid"].ToString(),
                    Title = item["room"]["title"].ToString(),
                    Cover = item["room"]["cover"]["url_list"][0].ToString(),
                    UserName = item["room"]["owner"]["nickname"].ToString(),
                    Online = item["room"]["room_view_stats"]?["display_value"]?.ToObject<int>() ?? 0,
                };
                items.Add(roomItem);
            }
            return new LiveCategoryResult()
            {
                HasMore = hasMore,
                Rooms = items
            };
        }
        public async Task<LiveCategoryResult> GetRecommendRooms(int page = 1)
        {
            var reqParams = new Dictionary<string, string> {
                    {"aid","6383"},
                    {"app_name","douyin_web" },
                    {"live_id", "1"},
                    {"device_platform","web" },
                    { "language", "zh-CN"},
                    { "enter_from", "link_share"},
                    { "cookie_enabled", "true"},
                    { "screen_width", "1980"},
                    { "screen_height", "1080"},
                    { "browser_language", "zh-CN"},
                    { "browser_platform", "Win32"},
                    { "browser_name", "Edge"},
                    { "browser_version", "125.0.0.0"},
                    {"browser_online", "true"},
                    { "count","15" },
                    { "offset", ((page - 1) * 15).ToString()},
                    {"partition","720" },
                    {"partition_type","1"},
                    {"req_from","2" }
                };
            var url = $"https://live.douyin.com/webcast/web/partition/detail/room/v2/?{Utils.BuildQueryString(reqParams)}";

            var requestUrl = await GetABougs(url);
            var resp = await HttpUtil.GetString(requestUrl,
                headers: await GetRequestHeaders()
            );
            Trace.WriteLine($"Douyin.GetRecommendRooms url: {requestUrl}");
            if (string.IsNullOrWhiteSpace(resp) || !resp.TrimStart().StartsWith("{"))
            {
                Trace.WriteLine($"Douyin.GetRecommendRooms invalid response: {resp}");
                return new LiveCategoryResult()
                {
                    HasMore = false,
                    Rooms = new List<LiveRoomItem>()
                };
            }
            var json = JObject.Parse(resp);
            var hasMore = (json["data"]["data"] as JArray).Count >= 15;
            var items = new List<LiveRoomItem>();
            foreach (var item in json["data"]["data"])
            {
                var roomItem = new LiveRoomItem()
                {
                    RoomID = item["web_rid"].ToString(),
                    Title = item["room"]["title"].ToString(),
                    Cover = item["room"]["cover"]["url_list"][0].ToString(),
                    UserName = item["room"]["owner"]["nickname"].ToString(),
                    Online = item["room"]["room_view_stats"]?["display_value"]?.ToObject<int>() ?? 0,
                };
                items.Add(roomItem);
            }
            return new LiveCategoryResult()
            {
                HasMore = hasMore,
                Rooms = items
            };
        }
        public async Task<LiveRoomDetail> GetRoomDetail(object roomId)
        {
            // There are two types of IDs: webRid and roomId
            // roomId is temporary, user gets a new roomId each time they start streaming
            // roomId is usually 19 digits, e.g.: 7376429659866598196
            // webRid is fixed, user has the same webRid for each stream
            // webRid is usually 11-12 digits, e.g.: 416144012050
            // Simple check: if length <= 16, treat as webRid
            if (roomId.ToString().Length <= 16)
            {
                var webRid = roomId as string;
                return await GetRoomDetailByWebRid(webRid);
            }

            return await GetRoomDetailByRoomID(roomId as string);
        }
        /// <summary>
        /// Get room detail by RoomId
        /// </summary>
        /// <param name="roomId">
        /// roomId is temporary, user gets a new roomId each time they start streaming.
        /// roomId is usually 19 digits, e.g.: 7376429659866598196
        /// </param>
        /// <returns></returns>
        private async Task<LiveRoomDetail> GetRoomDetailByRoomID(string roomId)
        {
            var roomData = await GetRoomDataByRoomID(roomId);
            // Get WebRid from room info
            var webRid = roomData["data"]["room"]["owner"]["web_rid"].ToString();
            // Get user unique ID for danmaku
            // Random number seems to work fine
            var userUniqueId = GenerateRandomNumber(12).ToString();
            var room = roomData["data"]["room"];
            var owner = room["owner"];
            var status = room["status"].ToObject<int>();
            // roomId is temporary, if status is 4 (not live), get room info by webRid
            if (status == 4)
            {
                var result = await GetRoomDetailByWebRid(webRid);
                return result;
            }
            var roomStatus = status == 2;
            // Need to get cookie for danmaku websocket
            var headers = await GetRequestHeaders(forceRefresh: true);
            return new LiveRoomDetail()
            {
                RoomID = webRid,
                Title = room["title"].ToString(),
                Cover = roomStatus ? room["cover"]["url_list"][0].ToString() : "",
                UserName = owner["nickname"].ToString(),
                UserAvatar = owner["avatar_thumb"]["url_list"][0].ToString(),
                Online = roomStatus
                  ? (room["room_view_stats"]?["display_value"]?.ToObject<int>() ?? 0)
                  : 0,
                Status = roomStatus,
                Url = $"https://live.douyin.com/{webRid}",
                Introduction = owner?["signature"]?.ToString() ?? "",
                Notice = "",
                DanmakuData = new DouyinDanmakuArgs()
                {
                    WebRid = webRid,
                    RoomId = roomId,
                    UserId = userUniqueId,
                    Cookie = headers["Cookie"],
                },
                Data = roomStatus ? room["stream_url"] : null,
            };

        }

        /// <summary>
        /// Get room detail by webRid
        /// </summary>
        /// <param name="webRid">
        /// webRid is fixed, user has the same webRid for each stream.
        /// webRid is usually 11-12 digits, e.g.: 416144012050
        /// </param>
        /// <returns></returns>
        private async Task<LiveRoomDetail> GetRoomDetailByWebRid(string webRid)
        {
            try
            {
                var result = await GetRoomDetailByWebRidApi(webRid);
                return result;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return await GetRoomDetailByWebRidHtml(webRid);
        }

        private async Task<LiveRoomDetail> GetRoomDetailByWebRidApi(string webRid)
        {
            Trace.WriteLine($"========== GetRoomDetailByWebRidApi Start ==========");
            Trace.WriteLine($"[RoomDetail] webRid={webRid}");
            
            // Get room data
            var data = await GetRoomDataApi(webRid);
            var roomData = data["data"][0];

            var userData = data["user"];
            var roomId = roomData["id_str"].ToString();
            Trace.WriteLine($"[RoomDetail] roomId={roomId}");

            // Get user unique ID for danmaku
            // Seems random number works fine
            var userUniqueId = GenerateRandomNumber(12).ToString();
            Trace.WriteLine($"[RoomDetail] userUniqueId={userUniqueId}");

            var owner = roomData["owner"];

            var roomStatus = roomData["status"].ToObject<int>() == 2;
            Trace.WriteLine($"[RoomDetail] roomStatus={roomStatus}");

            // Need to get cookie for danmaku websocket
            Trace.WriteLine($"[RoomDetail] Getting Cookie (forceRefresh=true)...");
            var headers = await GetRequestHeaders(forceRefresh: true);
            var cookie = headers.ContainsKey("Cookie") ? headers["Cookie"] : "";
            Trace.WriteLine($"[RoomDetail] Cookie length={cookie.Length}");
            Trace.WriteLine($"[RoomDetail] Cookie preview={cookie.Substring(0, Math.Min(100, cookie.Length))}...");
            
            Trace.WriteLine($"========== GetRoomDetailByWebRidApi Done ==========");
            return new LiveRoomDetail()
            {
                RoomID = webRid,
                Title = roomData["title"].ToString(),
                Cover = roomStatus ? roomData["cover"]["url_list"][0].ToString() : "",
                UserName = roomStatus
                    ? owner["nickname"].ToString()
                    : userData["nickname"].ToString(),
                UserAvatar = roomStatus
                    ? owner["avatar_thumb"]["url_list"][0].ToString()
                    : userData["avatar_thumb"]["url_list"][0].ToString(),
                Online = roomStatus
                    ? (roomData["room_view_stats"]?["display_value"]?.ToObject<int>() ?? 0)
                    : 0,
                Status = roomStatus,
                Url = $"https://live.douyin.com/{webRid}",
                Introduction = owner?["signature"]?.ToString() ?? "",
                Notice = "",
                DanmakuData = new DouyinDanmakuArgs()
                {
                    WebRid = webRid,
                    RoomId = roomId,
                    UserId = userUniqueId,
                    Cookie = headers["Cookie"],
                },
                Data = roomStatus ? roomData["stream_url"] : null,
            };

        }

        private async Task<LiveRoomDetail> GetRoomDetailByWebRidHtml(string webRid)
        {
            var roomData = await GetRoomDataHtml(webRid);
            var roomId = roomData["roomStore"]["roomInfo"]["room"]["id_str"].ToString();
            var userUniqueId =
                roomData["userStore"]["odin"]["user_unique_id"].ToString();

            var room = roomData["roomStore"]["roomInfo"]["room"];
            var owner = room["owner"];
            var anchor = roomData["roomStore"]["roomInfo"]["anchor"];
            var roomStatus = room["status"].ToObject<int>() == 2;

            // Need to get cookie for danmaku websocket
            var headers = await GetRequestHeaders(forceRefresh: true);
            return new LiveRoomDetail()
            {
                RoomID = webRid,
                Title = room["title"].ToString(),
                Cover = roomStatus ? room["cover"]["url_list"][0].ToString() : "",
                UserName = roomStatus
                    ? owner["nickname"].ToString()
                    : anchor["nickname"].ToString(),
                UserAvatar = roomStatus
                    ? owner["avatar_thumb"]["url_list"][0].ToString()
                    : anchor["avatar_thumb"]["url_list"][0].ToString(),
                Online = roomStatus
                    ? (room["room_view_stats"]?["display_value"]?.ToObject<int>() ?? 0)
                    : 0,
                Status = roomStatus,
                Url = $"https://live.douyin.com/{webRid}",
                Introduction = owner?["signature"]?.ToString() ?? "",
                Notice = "",
                DanmakuData = new DouyinDanmakuArgs()
                {
                    WebRid = webRid,
                    RoomId = roomId,
                    UserId = userUniqueId,
                    Cookie = headers["Cookie"],
                },
                Data = roomStatus ? room["stream_url"] : null,
            };
        }
        /// <summary>
        /// Get cookie before entering live room
        /// </summary>
        /// <param name="webRid">Live room RID</param>
        /// <returns></returns>
        private async Task<string> GetWebCookie(string webRid)
        {
            try
            {
                var resp = await HttpUtil.Head($"https://live.douyin.com/{webRid}",
                    headers: await GetRequestHeaders()
                );
                var dyCookie = "";
                foreach (var item in resp.Headers.GetValues("Set-Cookie"))
                {
                    var cookie = item.Split(';')[0];
                    if (cookie.Contains("ttwid") || cookie.Contains("__ac_nonce") || cookie.Contains("msToken"))
                    {
                        dyCookie += $"{cookie};";
                    }
                }
                
                // 如果没有获取到Cookie，使用默认Cookie
                if (string.IsNullOrEmpty(dyCookie))
                {
                    dyCookie = DEFAULT_COOKIE;
                }
                
                return dyCookie;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"GetWebCookie error: {ex.Message}");
                // 发生异常时返回默认Cookie
                return DEFAULT_COOKIE;
            }
        }

        /// <summary>
        /// Get user unique ID (deprecated)
        /// </summary>
        /// <param name="webRid"></param>
        /// <returns></returns>
        private async Task<string> GetUserUniqueId(string webRid)
        {
            var webInfo = await GetRoomDataHtml(webRid);
            return webInfo["userStore"]["odin"]["user_unique_id"].ToString();
        }

        private async Task<JToken> GetRoomDataHtml(string webRid)
        {
            var requestHeaders = await GetRequestHeaders();
            var resp = await HttpUtil.GetString($"https://live.douyin.com/{webRid}",
                headers: requestHeaders
            );
            Regex regex = new Regex("\\{\\\\\"state\\\\\":\\{\\\\\"appStore.*?\\]\\\\n", RegexOptions.Singleline);
            Match match = regex.Match(resp);
            string json = match.Success ? match.Groups[0].Value : "";
            if (string.IsNullOrEmpty(json))
            {
                throw new Exception("Unable to get room data");
            }
            json = json.Trim().Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("]\\n", "");
            return JObject.Parse(json)["state"];
        }

        private async Task<JToken> GetRoomDataApi(string webRid)
        {
            var reqParams = new Dictionary<string, string> {
                    {"aid","6383" },
                    {"app_name","douyin_web" },
                    {"live_id","1" },
                    {"device_platform","web" },
                    {"language","zh-CN" },
                    {"browser_language","zh-CN" },
                    {"browser_platform","Win32" },
                    {"browser_name","Chrome" },
                    {"browser_version","125.0.0.0" },
                    {"web_rid",webRid },
                    {"msToken","" }
                };
            var url = $"https://live.douyin.com/webcast/room/web/enter/?{Utils.BuildQueryString(reqParams)}";

            var requestHeaders = await GetRequestHeaders();
            // 使用动态 Referer（包含房间号，参考 DouyinLiveRecorder / dart_simple_live）
            requestHeaders["Referer"] = $"https://live.douyin.com/{webRid}";

            var requestUrl = await GetABougs(url);
            var resp = await HttpUtil.GetString(requestUrl,
                headers: requestHeaders
            );


           
            return JObject.Parse(resp)["data"];
        }

        private async Task<JToken> GetRoomDataByRoomID(string roomId)
        {
            var resp = await HttpUtil.GetString($"https://webcast.amemv.com/webcast/room/reflow/info/",
                headers: await GetRequestHeaders(),
                queryParameters: new Dictionary<string, string>
                {
                    {"type_id","0" },
                    {"live_id","1" },
                    {"room_id",roomId },
                    {"sec_user_id","" },
                    {"version_code","99.99.99" },
                    {"app_id","6383" },
                }
            );
            return JObject.Parse(resp);
        }

        public Task<List<LivePlayQuality>> GetPlayQuality(LiveRoomDetail roomDetail)
        {
            List<LivePlayQuality> qualities = new List<LivePlayQuality>();
            if (roomDetail.Data == null)
            {
                return Task.FromResult(qualities);
            }
            var data = roomDetail.Data as JToken;
            var qulityList = data["live_core_sdk_data"]["pull_data"]["options"]["qualities"];
            var streamData = data["live_core_sdk_data"]["pull_data"]["stream_data"].ToString();

            if (!streamData.StartsWith("{"))
            {
                var flvList = (data["flv_pull_url"] as JToken).Values().Select(c => c.ToString()).ToList();
                var hlsList = (data["hls_pull_url_map"] as JToken).Values().Select(c => c.ToString()).ToList();
                foreach (var quality in qulityList)
                {
                    int level = quality["level"].ToObject<int>();
                    List<String> urls = new List<string>();
                    var flvIndex = flvList.Count - level;
                    if (flvIndex >= 0 && flvIndex < flvList.Count)
                    {
                        urls.Add(flvList[flvIndex]);
                    }
                    var hlsIndex = hlsList.Count - level;
                    if (hlsIndex >= 0 && hlsIndex < hlsList.Count)
                    {
                        urls.Add(hlsList[hlsIndex]);
                    }
                    var qualityItem = new LivePlayQuality()
                    {
                        Quality = quality["name"].ToString(),
                        Sort = level,
                        Data = urls,
                    };
                    if (urls.Count > 0)
                    {
                        qualities.Add(qualityItem);
                    }
                }
            }
            else
            {
                var qualityData = JObject.Parse(streamData)["data"] as JObject;
                foreach (var quality in qulityList)
                {
                    List<string> urls = new List<string>();

                    var flvUrl =
                        qualityData[quality["sdk_key"].ToString()]?["main"]?["flv"]?.ToString();

                    if (flvUrl != null && flvUrl.Length > 0)
                    {
                        urls.Add(flvUrl);
                    }
                    var hlsUrl =
                        qualityData[quality["sdk_key"].ToString()]?["main"]?["hls"]?.ToString();
                    if (hlsUrl != null && hlsUrl.Length > 0)
                    {
                        urls.Add(hlsUrl);
                    }
                    var qualityItem = new LivePlayQuality()
                    {
                        Quality = quality["name"].ToString(),
                        Sort = quality["level"].ToObject<int>(),
                        Data = urls,
                    };
                    if (urls.Count > 0)
                    {
                        qualities.Add(qualityItem);
                    }
                }
            }
            // var qualityData = json.decode(
            //     detail.data["live_core_sdk_data"]["pull_data"]["stream_data"])["data"];

            //qualities.sort((a, b) => b.sort.compareTo(a.sort));
            qualities = qualities.OrderByDescending(q => q.Sort).ToList();
            return Task.FromResult(qualities);
        }

        public Task<List<string>> GetPlayUrls(LiveRoomDetail roomDetail, LivePlayQuality qn)
        {
            return Task.FromResult(qn.Data as List<string>);
        }

        /// <summary>
        /// 搜索直播间 - 支持房间号、直播链接、短链接
        /// </summary>
        public async Task<LiveSearchResult> Search(string keyword, int page = 1)
        {
            // 只处理第一页，因为是单个房间查询
            if (page > 1)
            {
                return new LiveSearchResult() { HasMore = false, Rooms = new List<LiveRoomItem>() };
            }

            var roomId = await ParseRoomId(keyword);
            if (string.IsNullOrEmpty(roomId))
            {
                return new LiveSearchResult() { HasMore = false, Rooms = new List<LiveRoomItem>() };
            }

            try
            {
                var detail = await GetRoomDetail(roomId);
                var items = new List<LiveRoomItem>
                {
                    new LiveRoomItem()
                    {
                        RoomID = detail.RoomID,
                        Title = detail.Title,
                        Cover = detail.Cover,
                        UserName = detail.UserName,
                        Online = detail.Online,
                    }
                };
                return new LiveSearchResult() { HasMore = false, Rooms = items };
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[Search] 获取房间信息失败: {ex.Message}");
                return new LiveSearchResult() { HasMore = false, Rooms = new List<LiveRoomItem>() };
            }
        }

        /// <summary>
        /// 解析房间号 - 支持多种格式
        /// </summary>
        private async Task<string> ParseRoomId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            input = input.Trim();

            // 1. 纯数字房间号
            if (Regex.IsMatch(input, @"^\d+$"))
            {
                return input;
            }

            // 2. 抖音直播链接: https://live.douyin.com/123456789
            var liveMatch = Regex.Match(input, @"live\.douyin\.com/(\d+)");
            if (liveMatch.Success)
            {
                return liveMatch.Groups[1].Value;
            }

            // 3. 抖音短链接: https://v.douyin.com/xxxxx
            if (input.Contains("v.douyin.com"))
            {
                try
                {
                    var realUrl = await ResolveShortUrl(input);
                    if (!string.IsNullOrEmpty(realUrl))
                    {
                        var match = Regex.Match(realUrl, @"live\.douyin\.com/(\d+)");
                        if (match.Success)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[ParseRoomId] 解析短链接失败: {ex.Message}");
                }
            }

            // 4. 从文本中提取链接
            var urlMatch = Regex.Match(input, @"https?://[^\s]+");
            if (urlMatch.Success && urlMatch.Value != input)
            {
                return await ParseRoomId(urlMatch.Value);
            }

            return null;
        }

        /// <summary>
        /// 解析抖音短链接
        /// </summary>
        private async Task<string> ResolveShortUrl(string shortUrl)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
                    var response = await client.GetAsync(shortUrl);
                    if (response.Headers.Location != null)
                    {
                        return response.Headers.Location.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[ResolveShortUrl] 异常: {ex.Message}");
            }
            return null;
        }

        public async Task<LiveStatusType> GetLiveStatus(object roomId)
        {
            var result = await GetRoomDetail(roomId: roomId);
            return result.Status ? LiveStatusType.Live : LiveStatusType.Offline;
        }
        public Task<List<LiveSuperChatMessage>> GetSuperChatMessages(object roomId)
        {
            return Task.FromResult(new List<LiveSuperChatMessage>());
        }

        private string GenerateRandomNumber(int length)
        {
            var random = new Random();
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                // First digit should not be 0
                if (i == 0)
                {
                    sb.Append(random.Next(1, 9));
                }
                else
                {
                    sb.Append(random.Next(0, 9));
                }
            }
            return sb.ToString();
        }

        private async Task<string> GetABougs(string url)
        {
            Trace.WriteLine($"[GetABougs] Start signing");
            try
            {
                var uri = new Uri(url);
                var baseUrl = uri.GetLeftPart(UriPartial.Path);
                var rawQuery = uri.Query.TrimStart('?');
                var msToken = GenerateMsToken();
                var queryForSign = string.IsNullOrEmpty(rawQuery)
                    ? $"msToken={msToken}"
                    : $"{rawQuery}&msToken={msToken}";

                Trace.WriteLine($"[GetABougs] queryForSign length={queryForSign.Length}");
                Trace.WriteLine($"[GetABougs] Calling DouyinABogusHelper.GenerateAsync...");
                
                var aBogus = await DouyinABogusHelper.GenerateAsync(queryForSign, USER_AGENT).ConfigureAwait(false);
                
                Trace.WriteLine($"[GetABougs] a_bogus result: '{aBogus}'");
                Trace.WriteLine($"[GetABougs] a_bogus length: {aBogus?.Length ?? 0}");
                Trace.WriteLine($"[GetABougs] a_bogus isEmpty: {string.IsNullOrEmpty(aBogus)}");
                
                if (string.IsNullOrEmpty(aBogus))
                {
                    Trace.WriteLine("[GetABougs] Warning: a_bogus is empty, using unsigned URL");
                    var fallbackQuery = string.IsNullOrEmpty(rawQuery)
                        ? $"msToken={Uri.EscapeDataString(msToken)}"
                        : $"{rawQuery}&msToken={Uri.EscapeDataString(msToken)}";
                    return $"{baseUrl}?{fallbackQuery}";
                }

                var finalQuery = string.IsNullOrEmpty(rawQuery)
                    ? $"msToken={Uri.EscapeDataString(msToken)}&a_bogus={Uri.EscapeDataString(aBogus)}"
                    : $"{rawQuery}&msToken={Uri.EscapeDataString(msToken)}&a_bogus={Uri.EscapeDataString(aBogus)}";

                Trace.WriteLine($"[GetABougs] Sign success");
                return $"{baseUrl}?{finalQuery}";
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[GetABougs] Exception: {ex.Message}");
                Trace.WriteLine($"[GetABougs] StackTrace: {ex.StackTrace}");
                return url;
            }
        }

        private static string GenerateMsToken(int length = 107)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var buffer = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[buffer[i] % chars.Length]);
            }
            return sb.ToString();
        }
    }
}

