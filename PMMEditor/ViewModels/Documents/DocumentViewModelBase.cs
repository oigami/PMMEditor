using PMMEditor.MVVM;

namespace PMMEditor.ViewModels.Documents
{
    public abstract class DocumentViewModelBase : BindableDisposableBase
    {
        #region Title変更通知プロパティ

        public abstract string Title { get; }

        #endregion

        #region ContentId変更通知プロパティ

        public abstract string ContentId { get; }

        #endregion

        #region IsSelected変更通知プロパティ

        private bool _IsSelected;

        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }
        }

        #endregion

        #region IsAcitive変更通知プロパティ

        private bool _IsAcitive;

        public bool IsAcitive
        {
            get { return _IsAcitive; }
            set { SetProperty(ref _IsAcitive, value); }
        }

        #endregion
    }
}
