﻿using System;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Panes
{
    public class AccessoryViewModel : PaneViewModelBase
    {
        private readonly Model _model;

        public AccessoryViewModel(Model _model)
        {
            this._model = _model;
            _model.ObserveProperty(_ => _.PmmStruct).Subscribe(_ => RaisePropertyChanged(nameof(PmmStruct)))
                  .AddTo(CompositeDisposables);
        }

        public override string Title { get; } = "Accessory";

        public override string ContentId { get; } = typeof(AccessoryViewModel).FullName;

        public PmmStruct PmmStruct => _model?.PmmStruct;

        #region SelectedAccessory変更通知プロパティ

        private PmmStruct.AccessoryData _selectedAccessory;

        public PmmStruct.AccessoryData SelectedAccessory
        {
            get { return _selectedAccessory; }
            set { SetProperty(ref _selectedAccessory, value); }
        }

        #endregion
    }
}
