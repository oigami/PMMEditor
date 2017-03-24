using System;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Panes
{
    public class CameraViewModel : PaneViewModelBase
    {
        private readonly Model _model;

        public CameraViewModel(Model _model)
        {
            this._model = _model;
            _model.ObserveProperty(_ => _.PmmStruct).Subscribe(_ => RaisePropertyChanged(nameof(PmmStruct)))
                  .AddTo(CompositeDisposables);
        }

        public override string Title { get; } = "Camera";

        public override string ContentId { get; } = typeof(CameraViewModel).FullName;

        public PmmStruct PmmStruct => _model?.PmmStruct;
    }
}
