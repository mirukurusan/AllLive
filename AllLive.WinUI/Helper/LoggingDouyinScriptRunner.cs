using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AllLive.Core.Helper;

namespace AllLive.WinUI.Helper
{
    internal sealed class LoggingDouyinScriptRunner : IDouyinScriptRunner
    {
        private readonly IDouyinScriptRunner _inner;

        public LoggingDouyinScriptRunner(IDouyinScriptRunner inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<string> EvaluateSignatureAsync(string msStub, string userAgent)
        {
            var result = await _inner.EvaluateSignatureAsync(msStub, userAgent).ConfigureAwait(false);
            LogHelper.Log("DouyinScript signature result: " + (result ?? string.Empty), LogType.DEBUG);
            return result;
        }

        public async Task<string> GenerateABogusAsync(string queryString, string userAgent)
        {
            var result = await _inner.GenerateABogusAsync(queryString, userAgent).ConfigureAwait(false);
            LogHelper.Log("DouyinScript a_bogus result: " + (result ?? string.Empty), LogType.DEBUG);
            return result;
        }

        public static async Task<string> ReadScriptsAsync()
        {
            try
            {
                var assembly = typeof(DouyinSignHelper).GetTypeInfo().Assembly;
                LogHelper.Log("LoggingDouyinScriptRunner loading scripts from assembly: " + assembly.FullName, LogType.DEBUG);
                foreach (var name in assembly.GetManifestResourceNames())
                {
                    LogHelper.Log("LoggingDouyinScriptRunner resource: " + name, LogType.DEBUG);
                }

                string ReadResource(string suffix)
                {
                    var resourceName = FindResource(assembly, suffix);
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var reader = new StreamReader(stream ?? throw new InvalidOperationException($"Missing resource: {resourceName}"), Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }

                var webMsSdk = ReadResource("webmssdk.js");
                var aBogus = ReadResource("a_bogus.js");
                return webMsSdk + "\n" + aBogus;
            }
            catch (Exception ex)
            {
                LogHelper.Log("LoggingDouyinScriptRunner ReadScriptsAsync error: " + ex, LogType.ERROR, ex);
                return string.Empty;
            }
        }

        private static string FindResource(Assembly assembly, string suffix)
        {
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (resource.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return resource;
                }
            }

            throw new FileNotFoundException($"Resource {suffix} not found in {assembly.FullName}.");
        }
    }
}
