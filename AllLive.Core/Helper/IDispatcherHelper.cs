using System;
using System.Threading.Tasks;

namespace AllLive.Core.Helper
{
    public interface IDispatcherHelper
    {
        bool HasThreadAccess { get; }
        Task RunOnUIThreadAsync(Action action);
    }
}
