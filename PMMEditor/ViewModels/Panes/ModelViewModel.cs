using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Livet;
using Livet.EventListeners;
using PMMEditor.Models;

namespace PMMEditor.ViewModels.Panes
{
    public class ModelViewModel : PaneViewModelBase
    {
        private readonly Model _model;
        private PropertyChangedEventListener _listener;

        public ModelViewModel(Model _model)
        {
            this._model = _model;
            _listener = new PropertyChangedEventListener(_model)
            {
                nameof(_model.PmmStruct),
                (_, __) =>
                {
                    RaisePropertyChanged(nameof(PmmStruct));
                    RaisePropertyChanged(nameof(ModelList));
                }
            };
        }

        public override string Title { get; } = "Model";

        public override string ContentId { get; } = typeof(ModelViewModel).FullName;

        public PmmStruct PmmStruct => _model?.PmmStruct;

        #region ModelList変更通知プロパティ

        public List<PmmStruct.ModelData> ModelList => _model.PmmStruct?.ModelDatas;

        #endregion ModelList変更通知プロパティ

        #region SelectedModel変更通知プロパティ

        private PmmStruct.ModelData _SelectedModel;

        public PmmStruct.ModelData SelectedModel
        {
            get { return _SelectedModel; }

            set
            {
                if (_SelectedModel == value)
                {
                    return;
                }
                _SelectedModel = value;
                RaisePropertyChanged();
            }
        }

        #endregion SelectedModel変更通知プロパティ
    }
}
