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

        private bool _IsSelected;

        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }
        }

        #endregion

        #region IsActive変更通知プロパティ

        private bool _IsActive;

        public bool IsActive
        {
            get { return _IsActive; }
            set { SetProperty(ref _IsActive, value); }
        }

        #endregion

        #region CanHide変更通知プロパティ

        private bool _CanHide = true;

        public bool CanHide
        {
            get { return _CanHide; }
            set { SetProperty(ref _CanHide, value); }
        }

        #endregion

        #region CanClose変更通知プロパティ

        private bool _CanClose;

        public bool CanClose
        {
            get { return _CanClose; }
            set { SetProperty(ref _CanClose, value); }
        }

        #endregion

        #region Visibility変更通知プロパティ

        private Visibility _Visibility;

        public Visibility Visibility
        {
            get { return _Visibility; }
            set { SetProperty(ref _Visibility, value); }
        }

        #endregion
    }
}
