using System;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Panes
{
    public class HeaderViewModel : PaneViewModelBase
    {
        private readonly Model _model;

        public HeaderViewModel(Model _model)
        {
            this._model = _model;
            _model.ObserveProperty(_ => _.PmmStruct).Subscribe(_ => RaisePropertyChanged(nameof(PmmStruct)))
                  .AddTo(CompositeDisposable);
        }

        public override string Title { get; } = "Header";

        public override string ContentId { get; } = typeof(HeaderViewModel).FullName;

        public PmmStruct PmmStruct => _model?.PmmStruct;
    }
}
