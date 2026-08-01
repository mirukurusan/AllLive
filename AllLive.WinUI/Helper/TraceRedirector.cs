using Windows.ApplicationModel;
using Microsoft.UI;
using AllLive.Core.Helper;
﻿using System.Diagnostics;

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
            public override void Write(string message)
            {
                LogHelper.Log(message, LogType.DEBUG);
            }

            public override void WriteLine(string message)
            {
                LogHelper.Log(message, LogType.DEBUG);
            }
        }
    }
}
