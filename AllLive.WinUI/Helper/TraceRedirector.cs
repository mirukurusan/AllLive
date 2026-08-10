using System;
using System.Diagnostics;

namespace AllLive.WinUI.Helper
{
    internal static class TraceRedirector
    {
        private static bool _initialized;
        private static readonly LogHelperTraceListener _listener = new LogHelperTraceListener();

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            if (!Trace.Listeners.Contains(_listener))
            {
                Trace.Listeners.Add(_listener);
            }
        }

        private sealed class LogHelperTraceListener : TraceListener
        {
            // 防止无限递归：LogHelper.Log -> Debug.WriteLine -> Trace -> 本监听器 -> LogHelper.Log ...
            [ThreadStatic]
            private static bool _inLog;

            public override void Write(string message)
            {
                LogToHelper(message);
            }

            public override void WriteLine(string message)
            {
                LogToHelper(message);
            }

            private void LogToHelper(string message)
            {
                if (_inLog)
                {
                    return;
                }
                _inLog = true;
                try
                {
                    LogHelper.Log(message, LogType.DEBUG);
                }
                finally
                {
                    _inLog = false;
                }
            }
        }
    }
}
