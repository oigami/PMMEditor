using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Livet.Commands;
using Livet.Messaging.IO;
using PMMEditor.Log;
using PMMEditor.Models;
using PMMEditor.MVVM;
using PMMEditor.ViewModels.Documents;
using PMMEditor.ViewModels.Graphics;
using Reactive.Bindings;

namespace PMMEditor.ViewModels.MMD
{
    internal class MainViewViewModel : BindableViewModel
    {
        private readonly Model _model;

        public MainViewViewModel(Model model, LogMessageNotifier logger)
        {
            _model = model;
            logger.Subscribe(log => { MessageBox.Show(log.ToString()); });
            TimelineViewModel = new CameraLightAccessoryViewModel(_model);
            RendererViewModel = new MainRenderViewModel(_model);

            // モデル操作のためのデータ
            ModelAndCameraList = _model.MmdModelList.List.ToReadOnlyReactiveCollection();
            ModelDeleteCommand = new ViewModelCommand(() => _model.MmdModelList.Delete(SelectedModel));
            ModelLoadCommand = new ViewModelCommand(() =>
            {
                var message = new OpeningFileSelectionMessage("Open")
                {
                    Title = "",
                    Filter = "*.pmd|*.pmd|すべてのファイル|*",
                    MultiSelect = false
                };
                Messenger.Raise(message);
                var path = message.Response?[0];
                if (path == null)
                {
                    return;
                }
                _model.MmdModelList.Add(path);
            });

            ChangeModelCameraModeCommand = new ViewModelCommand(() =>
            {
                TimelineViewModel = new BoneTimelineViewModel(model, SelectedModel);
            });
        }

        private TimelineViewModelBase _timelineViewModel;

        public TimelineViewModelBase TimelineViewModel
        {
            get { return _timelineViewModel; }
            set { SetProperty(ref _timelineViewModel, value); }
        }

        private MainRenderViewModel _rendererViewModel;


        public MainRenderViewModel RendererViewModel
        {
            get { return _rendererViewModel; }
            set { SetProperty(ref _rendererViewModel, value); }
        }

        public ReadOnlyReactiveCollection<MmdModelModel> ModelAndCameraList { get; }

        public ViewModelCommand ModelDeleteCommand { get; }

        private MmdModelModel _selectedModel;

        public MmdModelModel SelectedModel
        {
            get { return _selectedModel; }
            set { SetProperty(ref _selectedModel, value); }
        }

        public ViewModelCommand ModelLoadCommand { get; }

        public ViewModelCommand ChangeModelCameraModeCommand
        {
            get;
        }
    }
}
