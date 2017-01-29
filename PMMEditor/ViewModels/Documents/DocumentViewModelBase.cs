using Livet;

namespace PMMEditor.ViewModels.Documents
{
    public abstract class DocumentViewModelBase : ViewModel
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
            set
            {
                if (_IsSelected == value)
                {
                    return;
                }
                _IsSelected = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region IsAcitive変更通知プロパティ

        private bool _IsAcitive;

        public bool IsAcitive
        {
            get { return _IsAcitive; }
            set
            {
                if (_IsAcitive == value)
                {
                    return;
                }
                _IsAcitive = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
