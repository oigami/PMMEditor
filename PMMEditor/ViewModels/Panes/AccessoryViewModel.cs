using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Livet;
using Livet.EventListeners;
using PMMEditor.Models;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Panes
{
    public class AccessoryViewModel : PaneViewModelBase
    {
        private readonly Model _model;
        private PropertyChangedEventListener _listener;

        public AccessoryViewModel(Model _model)
        {
            this._model = _model;
            _listener = new PropertyChangedEventListener(_model)
            {
                nameof(_model.PmmStruct),
                (_, __) => RaisePropertyChanged(nameof(PmmStruct))
            }.AddTo(CompositeDisposable);
        }

        public override string Title { get; } = "Accessory";

        public override string ContentId { get; } = typeof(AccessoryViewModel).FullName;

        public PmmStruct PmmStruct => _model?.PmmStruct;

        #region SelectedAccessory変更通知プロパティ

        private PmmStruct.AccessoryData _SelectedAccessory;

        public PmmStruct.AccessoryData SelectedAccessory
        {
            get { return _SelectedAccessory; }
            set
            {
                if (_SelectedAccessory == value)
                {
                    return;
                }
                _SelectedAccessory = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
