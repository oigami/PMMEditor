using System.Windows;
using Livet;

namespace PMMEditor.ViewModels.Panes
{
    public abstract class PaneViewModelBase : ViewModel
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

        #region IsActive変更通知プロパティ

        private bool _IsActive;

        public bool IsActive
        {
            get { return _IsActive; }
            set
            {
                if (_IsActive == value)
                {
                    return;
                }
                _IsActive = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region CanHide変更通知プロパティ

        private bool _CanHide = true;

        public bool CanHide
        {
            get { return _CanHide; }
            set
            {
                if (_CanHide == value)
                {
                    return;
                }
                _CanHide = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region CanClose変更通知プロパティ

        private bool _CanClose = false;

        public bool CanClose
        {
            get { return _CanClose; }
            set
            {
                if (_CanClose == value)
                {
                    return;
                }
                _CanClose = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Visibility変更通知プロパティ

        private Visibility _Visibility;

        public Visibility Visibility
        {
            get { return _Visibility; }
            set
            {
                if (_Visibility == value)
                {
                    return;
                }
                _Visibility = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
