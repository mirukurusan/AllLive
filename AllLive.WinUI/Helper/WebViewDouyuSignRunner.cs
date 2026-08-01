using AllLive.Core.Helper;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AllLive.WinUI.Helper
{
    /// <summary>
    /// Douyu sign generator using ClearScript V8 engine.
    /// Replaces the old UWP WebView-based implementation.
    /// </summary>
    public sealed class V8DouyuSignRunner : IDisposable, IDouyuSignRunner
    {
        private readonly object _initSync = new object();
        private V8ScriptEngine _engine;
        private bool _initialized;

        public V8DouyuSignRunner() { }

        public Task<string> GenerateSignAsync(string html, string rid)
        {
            return ExecuteInternalAsync(html ?? string.Empty, rid ?? string.Empty, retryOnFailure: true);
        }

        private async Task<string> ExecuteInternalAsync(string html, string rid, bool retryOnFailure)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(rid))
                    return string.Empty;

                var did = "10000000000000000000000000001501";
                var time = AllLive.Core.Helper.Utils.GetTimestamp();

                return await Task.Run(() =>
                {
                    lock (_initSync)
                    {
                        try
                        {
                            // Evaluate the HTML chunk which defines ub98484234()
                            _engine.Evaluate(html);

                            // Call the function to get the sign JS code
                            var jsCode = _engine.Evaluate("ub98484234()")?.ToString();
                            if (string.IsNullOrEmpty(jsCode))
                                return string.Empty;

                            var v = Regex.Match(jsCode, @"v=(\d+)").Groups[1].Value;
                            var rb = AllLive.Core.Helper.Utils.ToMD5(rid + did + time + v);

                            // Fix the obfuscated JS to expose sign()
                            var jsCode2 = Regex.Replace(jsCode, @"return rt;}\);?", "return rt;}");
                            jsCode2 = Regex.Replace(jsCode2, @"\(function \(", "function sign(");
                            jsCode2 = Regex.Replace(jsCode2, @"CryptoJS\.MD5\(cb\)\.toString\(\)", $@"""{rb}""");

                            _engine.Evaluate(jsCode2);

                            var escapedRid = EscapeJsString(rid);
                            var script = $"(function(){{ try {{ return sign('{escapedRid}','{did}','{time}'); }} catch(e) {{ return 'ERROR:' + e.message; }} }})()";
                            var result = _engine.Evaluate(script)?.ToString() ?? string.Empty;

                            return result;
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Log($"V8DouyuSignRunner error: {ex.Message}", LogType.ERROR, ex);
                            return string.Empty;
                        }
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (retryOnFailure)
                {
                    LogHelper.Log("V8DouyuSignRunner recoverable error, resetting V8 engine", LogType.DEBUG);
                    await ResetEngineAsync().ConfigureAwait(false);
                    return await ExecuteInternalAsync(html, rid, retryOnFailure: false).ConfigureAwait(false);
                }

                LogHelper.Log($"V8DouyuSignRunner error: {ex}", LogType.ERROR, ex);
                return string.Empty;
            }
        }

        private Task _initializationTask;

        private Task EnsureInitializedAsync()
        {
            if (_initialized) return Task.CompletedTask;

            lock (_initSync)
            {
                if (_initialized) return Task.CompletedTask;
                if (_initializationTask == null)
                    _initializationTask = InitializeAsync();
                return _initializationTask;
            }
        }

        private Task InitializeAsync()
        {
            return Task.Run(() =>
            {
                lock (_initSync)
                {
                    _engine = new V8ScriptEngine();
                    _engine.Execute(@"
                        var window = this;
                        var navigator = { userAgent: '' };
                        var document = { cookie: '' };
                        var location = { href: '' };
                    ");
                    _initialized = true;
                }
            });
        }

        private Task ResetEngineAsync()
        {
            lock (_initSync)
            {
                _initialized = false;
                _initializationTask = null;
                try { _engine?.Dispose(); } catch { }
                _engine = null;
            }
            return Task.CompletedTask;
        }

        private static string EscapeJsString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        public void Dispose()
        {
            lock (_initSync)
            {
                _initialized = false;
                try { _engine?.Dispose(); } catch { }
                _engine = null;
            }
        }
    }
}
