using AllLive.Core.Helper;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AllLive.WinUI.Helper
{
    /// <summary>
    /// Douyin (TikTok) JS signature runner using ClearScript V8 engine.
    /// Replaces the old UWP WebView-based implementation with a lightweight V8 engine.
    /// </summary>
    public sealed class V8DouyinScriptRunner : IDisposable, IDouyinScriptRunner
    {
        private readonly object _initSync = new object();
        private V8ScriptEngine _engine;
        private bool _initialized;
        private string _scripts;

        public V8DouyinScriptRunner() { }

        public Task<string> EvaluateSignatureAsync(string msStub, string userAgent)
        {
            return ExecuteScriptAsync("getMSSDKSignature", msStub, userAgent);
        }

        public Task<string> GenerateABogusAsync(string queryString, string userAgent)
        {
            return ExecuteScriptAsync("getABogus", queryString ?? string.Empty, userAgent ?? string.Empty);
        }

        private Task<string> ExecuteScriptAsync(string functionName, string arg1, string arg2)
        {
            return ExecuteScriptInternalAsync(functionName, arg1, arg2, retryOnFailure: true);
        }

        private async Task<string> ExecuteScriptInternalAsync(string functionName, string arg1, string arg2, bool retryOnFailure)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                var escapedArg1 = EscapeJsString(arg1);
                var escapedArg2 = EscapeJsString(arg2);
                var script = $"(function(){{ try {{ return {functionName}('{escapedArg1}','{escapedArg2}'); }} catch(e) {{ return 'ERROR:' + e.message; }} }})()";

                lock (_initSync)
                {
                    var result = _engine.Evaluate(script)?.ToString() ?? string.Empty;
                    if (result.StartsWith("ERROR:"))
                        throw new InvalidOperationException(result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                if (retryOnFailure && IsRecoverableException(ex))
                {
                    await ResetEngineAsync().ConfigureAwait(false);
                    return await ExecuteScriptInternalAsync(functionName, arg1, arg2, retryOnFailure: false).ConfigureAwait(false);
                }
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

        private async Task InitializeAsync()
        {
            try
            {
                var scripts = await LoggingDouyinScriptRunner.ReadScriptsAsync().ConfigureAwait(false);

                await Task.Run(() =>
                {
                    lock (_initSync)
                    {
                        _engine = new V8ScriptEngine();

                        // Set up browser-like globals that the scripts may reference
                        _engine.Execute(@"
                            var window = this;
                            var self = this;
                            var navigator = { userAgent: '', appName: 'Netscape', platform: 'Win32' };
                            var document = { cookie: '', referrer: '', location: { href: '' } };
                            var location = { href: '' };
                            var localStorage = { getItem: function(k) { return null; }, setItem: function(k,v) {} };
                            var sessionStorage = { getItem: function(k) { return null; }, setItem: function(k,v) {} };
                        ");

                        _engine.Evaluate(scripts);
                        _scripts = scripts;

                        // Verify exported functions
                        var checkABogus = _engine.Evaluate("typeof getABogus")?.ToString();
                        var checkSignature = _engine.Evaluate("typeof getMSSDKSignature")?.ToString();
                        if (checkABogus != "function")
                            LogHelper.Log("[V8Douyin] Warning: getABogus is not a function!", LogType.ERROR);

                        _initialized = true;
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[V8Douyin] Init error: {ex.Message}", LogType.ERROR, ex);
            }
            finally
            {
                if (!_initialized)
                {
                    lock (_initSync) { _initializationTask = null; }
                }
            }
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

        private static bool IsRecoverableException(Exception ex)
        {
            return ex is ScriptEngineException || ex is InvalidOperationException;
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
