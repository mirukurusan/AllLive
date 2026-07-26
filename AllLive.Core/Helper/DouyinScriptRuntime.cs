using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;

namespace AllLive.Core.Helper
{
    public interface IDouyinScriptRunner
    {
        Task<string> EvaluateSignatureAsync(string msStub, string userAgent);

        Task<string> GenerateABogusAsync(string queryString, string userAgent);
    }

    public static class DouyinScriptRuntime
    {
        private static IDouyinScriptRunner _current;

        public static IDouyinScriptRunner Current
        {
            get => _current;
            set => _current = value ?? throw new ArgumentNullException(nameof(value));
        }

        static DouyinScriptRuntime()
        {
            _current = new NullDouyinScriptRunner();
        }
    }

    internal sealed class NullDouyinScriptRunner : IDouyinScriptRunner
    {
        public Task<string> EvaluateSignatureAsync(string msStub, string userAgent) => Task.FromResult(string.Empty);

        public Task<string> GenerateABogusAsync(string queryString, string userAgent) => Task.FromResult(string.Empty);
    }
}



