using System.Windows;
using PMMEditor.MVVM;

namespace PMMEditor.ViewModels.Panes
{
    public abstract class PaneViewModelBase : BindableDisposableBase
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

        #region IsActive変更通知プロパティ

        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set { SetProperty(ref _isActive, value); }
        }

        #endregion

        #region CanHide変更通知プロパティ

        private bool _canHide = true;

        public bool CanHide
        {
            get { return _canHide; }
            set { SetProperty(ref _canHide, value); }
        }

        #endregion

        #region CanClose変更通知プロパティ

        private bool _canClose;

        public bool CanClose
        {
            get { return _canClose; }
            set { SetProperty(ref _canClose, value); }
        }

        #endregion

        #region Visibility変更通知プロパティ

        private Visibility _visibility;

        public Visibility Visibility
        {
            get { return _visibility; }
            set { SetProperty(ref _visibility, value); }
        }

        #endregion
    }
}
