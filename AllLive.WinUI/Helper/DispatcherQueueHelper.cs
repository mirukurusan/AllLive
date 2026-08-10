using System;
using System.Threading.Tasks;
using AllLive.Core.Helper;
using Microsoft.UI.Dispatching;

namespace AllLive.WinUI.Helper
{
    public class DispatcherQueueHelper : IDispatcherHelper
    {
        private readonly DispatcherQueue _queue;

        public DispatcherQueueHelper(DispatcherQueue queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public bool HasThreadAccess => _queue.HasThreadAccess;

        public Task RunOnUIThreadAsync(Action action)
        {
            if (_queue.HasThreadAccess)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            _queue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                try { action(); tcs.TrySetResult(true); }
                catch (Exception ex) { tcs.TrySetException(ex); }
            });
            return tcs.Task;
        }
    }
}
