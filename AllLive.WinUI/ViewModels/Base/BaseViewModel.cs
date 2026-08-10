using System;
using System.ComponentModel;
using System.Windows.Input;
using AllLive.Core.Helper;
using AllLive.WinUI.Helper;
using WinUIUtils = AllLive.WinUI.Helper.Utils;

namespace AllLive.WinUI.ViewModels
{
    public class BaseNotifyPropertyChanged:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void DoPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    public class BaseViewModel : BaseNotifyPropertyChanged
    {
        public IDispatcherHelper Dispatcher { get; set; }

        public BaseViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            LoadMoreCommand = new RelayCommand(LoadMore);
        }
        public ICommand LoadMoreCommand { get;  set; }
        public ICommand RefreshCommand { get;  set; }

        public int Page { get; set; } = 1;

        private bool _loading;
        public bool Loading
        {
            get { return _loading; }
            set { _loading = value; DoPropertyChanged("Loading"); }
        }

        private bool _canLoadMore;
        public bool CanLoadMore
        {
            get { return _canLoadMore; }
            set { _canLoadMore = value; DoPropertyChanged("CanLoadMore"); }
        }

        

        private bool _empty=false;
        public bool IsEmpty
        {
            get { return _empty; }
            set { _empty = value; DoPropertyChanged("IsEmpty"); }
        }


        public virtual void Refresh()
        {
            Page = 1;
        }
        public virtual void LoadMore()
        {

        }

        public virtual void HandleError(Exception ex,string message=null)
        {
            if (LogHelper.IsNetworkError(ex))
            {
                WinUIUtils.ShowMessageToast("请检查网络连接情况");
            }
            else
            {
                LogHelper.Log(ex.Message, LogType.ERROR, ex);
                // 未指定自定义提示时，直接展示异常信息，便于用户定位具体原因
                WinUIUtils.ShowMessageToast(string.IsNullOrEmpty(message) ? ex.Message : message);
            }
        }

    }
}
