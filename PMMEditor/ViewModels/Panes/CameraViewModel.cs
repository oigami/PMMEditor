using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Livet;
using Livet.EventListeners;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Panes
{
    public class CameraViewModel : PaneViewModelBase
    {
        private readonly Model _model;
        private PropertyChangedEventListener _listener;

        public CameraViewModel(Model _model)
        {
            this._model = _model;
            _listener = new PropertyChangedEventListener(_model)
            {
                nameof(_model.PmmStruct),
                (_, __) => RaisePropertyChanged(nameof(PmmStruct))
            }.AddTo(CompositeDisposable);
        }

        public override string Title { get; } = "Camera";

        public override string ContentId { get; } = typeof(CameraViewModel).FullName;

        public PmmStruct PmmStruct => _model?.PmmStruct;
    }
}
