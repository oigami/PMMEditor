using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Livet;
using Livet.EventListeners;
using PMMEditor.Models;

namespace PMMEditor.ViewModels.Panes
{
    public class HeaderViewModel : PaneViewModelBase
    {
        private readonly Model _model;
        private PropertyChangedEventListener _listener;

        public HeaderViewModel(Model _model)
        {
            this._model = _model;
            _listener = new PropertyChangedEventListener(_model)
            {
                nameof(_model.PmmStruct),
                (_, __) => RaisePropertyChanged(nameof(PmmStruct))
            };
        }

        public override string Title { get; } = "HeaderView";

        public override string ContentId { get; } = typeof(HeaderViewModel).FullName;

        public PmmStruct PmmStruct => _model?.PmmStruct;
    }
}
