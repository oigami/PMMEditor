using System;
using System.Collections.Generic;
using Livet.EventListeners;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Panes
{
    public class ModelViewModel : PaneViewModelBase
    {
        private readonly Model _model;

        public ModelViewModel(Model _model)
        {
            this._model = _model;
            _model.ObserveProperty(_ => _.PmmStruct).Subscribe(
                _ =>
                {
                    RaisePropertyChanged(nameof(PmmStruct));
                    RaisePropertyChanged(nameof(ModelList));
                }).AddTo(CompositeDisposables);
        }

        public override string Title { get; } = "Model";

        public override string ContentId { get; } = typeof(ModelViewModel).FullName;

        public PmmStruct PmmStruct => _model?.PmmStruct;

        #region ModelList変更通知プロパティ

        public List<PmmStruct.ModelData> ModelList => _model.PmmStruct?.ModelDatas;

        #endregion ModelList変更通知プロパティ

        #region SelectedModel変更通知プロパティ

        private PmmStruct.ModelData _selectedModel;

        public PmmStruct.ModelData SelectedModel
        {
            get { return _selectedModel; }
            set { SetProperty(ref _selectedModel, value); }
        }

        #endregion SelectedModel変更通知プロパティ
    }
}
