using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using Tup;

namespace AllLive.Core.Helper
{
    public class TupHttpHelper
    {
        private readonly string baseUrl = "";
        private readonly string servantName = "";
        readonly HttpClient httpClient;
        public TupHttpHelper(string baseUrl, string servantName, string userAgent = null, Dictionary<string, string> headers = null)
        {
            this.baseUrl = baseUrl;
            this.servantName = servantName;
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(baseUrl);
            if (!string.IsNullOrEmpty(userAgent))
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            }
            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }
        }

        public async Task<Resp> GetAsync<Req, Resp>(Req req, string function,Resp proxy)
        {
            Resp result = proxy;
            try
            {
                TarsUniPacket uniPacket = new TarsUniPacket();
                uniPacket.RequestId = 0;
                uniPacket.ServantName = servantName;
                uniPacket.FuncName = function;
                uniPacket.setTarsVersion(Const.PACKET_TYPE_TUP3);
                uniPacket.setTarsPacketType(Const.PACKET_TYPE_TARSNORMAL);
                uniPacket.Put("tReq", req);
                byte[] array = uniPacket.Encode();

                System.Diagnostics.Trace.WriteLine($"[TupHttpHelper] sending request to {baseUrl}, function: {function}, size: {array.Length}");

                var reqContent= new ByteArrayContent(array);
                reqContent.Headers.Add("Content-Type", "application/x-wup");
                reqContent.Headers.Add("Content-Length", array.Length.ToString());
                var response = await httpClient.PostAsync("", reqContent);

                System.Diagnostics.Trace.WriteLine($"[TupHttpHelper] response status: {response.StatusCode}");

                var responseBytes= await response.Content.ReadAsByteArrayAsync();

                System.Diagnostics.Trace.WriteLine($"[TupHttpHelper] response size: {responseBytes.Length}");
             
                TarsUniPacket respPack =new TarsUniPacket();
                respPack.Decode(responseBytes);
                var code = respPack.Get("", 0);

                System.Diagnostics.Trace.WriteLine($"[TupHttpHelper] response code: {code}");

                result = respPack.Get<Resp>("tRsp", result);
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[TupHttpHelper] error: {ex.Message}");
                return result;
            }
        }




    }
}
