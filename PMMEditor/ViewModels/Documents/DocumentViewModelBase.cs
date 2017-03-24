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

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        #endregion

        #region IsAcitive変更通知プロパティ

        private bool _isAcitive;

        public bool IsAcitive
        {
            get { return _isAcitive; }
            set { SetProperty(ref _isAcitive, value); }
        }

        #endregion
    }
}
