using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.MMD
{
    internal class MainViewViewModel : BindableViewModel
    {
        private readonly Model _model;

        public MainViewViewModel(Model model, LogMessageNotifier logger)
        {
            _model = model;
            logger.Subscribe(log => { MessageBox.Show(log.ToString()); });
            RendererViewModel = new MainRenderViewModel(_model);

            // モデル操作のためのデータ
            var modelList = _model.MmdModelList.List
                                  .ToReadOnlyReactiveCollection(
                                      _ => (TimelineViewModelBase) new BoneTimelineViewModel(model, _))
                                  .AddTo(CompositeDisposable);
            ModelAndCameraList =
                new ObservableCollection<TimelineViewModelBase> { new CameraLightAccessoryViewModel(_model) }
                    .MultiMerge(modelList).AddTo(CompositeDisposable);

            SelectedModel = ModelAndCameraList[0];

            ModelDeleteCommand = this.ObserveProperty(_ => _.SelectedModel).ToReactiveProperty()
                                     .Select(_ => _ is BoneTimelineViewModel)
                                     .ToReactiveCommand().AddTo(CompositeDisposable);

            ModelDeleteCommand.Subscribe(_ => _model.MmdModelList.Delete(((BoneTimelineViewModel) SelectedModel).Model))
                              .AddTo(CompositeDisposable);

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

            ChangeModelCameraModeCommand =
                new ViewModelCommand(() => { SelectedModel = ModelAndCameraList[0]; });
        }

        public IList<TimelineViewModelBase> ModelAndCameraList { get; }

        private MainRenderViewModel _rendererViewModel;


        public MainRenderViewModel RendererViewModel
        {
            get { return _rendererViewModel; }
            set { SetProperty(ref _rendererViewModel, value); }
        }

        public ReactiveCommand ModelDeleteCommand { get; }

        private TimelineViewModelBase _selectedModel;

        public TimelineViewModelBase SelectedModel
        {
            get { return _selectedModel; }
            set { SetProperty(ref _selectedModel, value); }
        }

        public ViewModelCommand ModelLoadCommand { get; }

        public ViewModelCommand ChangeModelCameraModeCommand { get; }
    }
}
