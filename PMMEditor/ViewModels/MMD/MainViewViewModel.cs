using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using PMMEditor.Log;
using PMMEditor.Models;
using PMMEditor.MVVM;
using PMMEditor.ViewModels.Documents;
using PMMEditor.ViewModels.Graphics;
using PMMEditor.Views;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.MMD
{
    public class LogMessageViewModel : BindableViewModel
    {
        public LogMessageViewModel(LogMessage log)
        {
            _log = log;
        }

        public string Message => _log.Message;

        public Exception Exception => _log.Exception;

        public LogLevel Level => _log.Level;

        private LogMessage _log;
    }

    internal class MainViewViewModel : BindableViewModel
    {
        private readonly Model _model;

        public MainViewViewModel(Model model, LogMessageNotifier logger)
        {
            _model = model;
            PrevFrameCommand = new ViewModelCommand(() => _model.FrameControlModel.PrevFrame());
            NextFrameCommand = new ViewModelCommand(() => _model.FrameControlModel.NextFrame());
            SwitchPlayAndStopCommand = new ViewModelCommand(() => _model.FrameControlModel.SwitchPlayAndStop());

            logger.Subscribe(log => Messenger.Raise(new TransitionMessage(new LogMessageViewModel(log), "Transition")));
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

            IsCharacterModelMode =
                this.ObserveProperty(_ => _.SelectedModel).Select(_ => _ is BoneTimelineViewModel)
                    .ToReactiveProperty().AddTo(CompositeDisposable);
            ModelDeleteCommand = IsCharacterModelMode.ToReactiveCommand().AddTo(CompositeDisposable);

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
                new ViewModelCommand(() =>
                {
                    if (IsCharacterModelMode.Value)
                    {
                        // キャラモードのときはカメラモードにする
                        _prevSelectedModel = (BoneTimelineViewModel) SelectedModel;
                        SelectedModel = ModelAndCameraList[0];
                    }
                    else if (_prevSelectedModel != null)
                    {
                        // 前回選択してたキャラに戻す
                        SelectedModel = _prevSelectedModel;
                    }
                    else
                    {
                        if (ModelAndCameraList.Count > 1)
                        {
                            // 前回選択してなかったときは最初のキャラをセットする
                            _prevSelectedModel = (BoneTimelineViewModel) ModelAndCameraList[1];
                            SelectedModel = _prevSelectedModel;
                        }
                        else
                        {
                            // キャラを読み込んでないときは読み込み処理に移行
                            ModelLoadCommand.Execute();
                        }
                    }
                });
        }

        public async void Open(OpeningFileSelectionMessage m)
        {
            if (m.Response == null)
            {
                return;
            }

            await _model.OpenPmm(m.Response[0]);
        }

        public ReactiveProperty<bool> IsCharacterModelMode { get; }

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

        private BoneTimelineViewModel _prevSelectedModel;

        public ViewModelCommand ModelLoadCommand { get; }

        public ViewModelCommand ChangeModelCameraModeCommand { get; }

        public ViewModelCommand NextFrameCommand { get; }

        public ViewModelCommand PrevFrameCommand { get; }

        public ViewModelCommand SwitchPlayAndStopCommand { get; }
    }
}
