using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Livet.Commands;
using Microsoft.Win32;
using PMMEditor.Log;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using PMMEditor.MVVM;
using PMMEditor.ViewModels.Documents;
using PMMEditor.ViewModels.Graphics;
using PMMEditor.ViewModels.Panes;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.MMW
{
    public class MainViewViewModel : BindableDisposableBase
    {
        private readonly Model _model;
        private readonly ILogger _logger;

        public MainViewViewModel()
        {
            _logger = new LogMessageNotifier();
            _model = new Model(_logger).AddTo(CompositeDisposables);
            SwitchPlayAndStopCommand = new ViewModelCommand(SwitchPlayAndStop);
            NextFrameCommand = new ViewModelCommand(NextFrame);
            PrevFrameCommand = new ViewModelCommand(PrevFrame);
        }

        public async void Initialize()
        {
            _model.ObserveProperty(_ => _.PmmStruct).Subscribe(_ => RaisePropertyChanged(nameof(PmmStruct)))
                  .AddTo(CompositeDisposables);
#if DEBUG
            try
            {
                await _model.OpenPmmAsync(File.ReadAllBytes("C:/tool/MikuMikuDance_v926x64/UserFile/サンプル（きしめんAllStar).pmm"));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n" + e.StackTrace);
            }
#endif

            AddPane(() => new HeaderViewModel(_model).AddTo(CompositeDisposables));
            AddPane(() => new ModelViewModel(_model).AddTo(CompositeDisposables));
            AddPane(() => new CameraViewModel(_model).AddTo(CompositeDisposables));
            AddPane(() => new AccessoryViewModel(_model).AddTo(CompositeDisposables));

            await AddDocument(() => Task.Run(() => new MainRenderViewModel(_model)), "test");
        }

        #region PmmStruct変更通知プロパティ

        public PmmStruct PmmStruct => _model.PmmStruct;

        #endregion

        #region OpenPmmCommand

        private ViewModelCommand _openPmmCommand;

        public ViewModelCommand OpenPmmCommand => _openPmmCommand ?? (_openPmmCommand = new ViewModelCommand(OpenPmm));

        private async void OpenPmm()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "pmm file(*.pmm)|*.pmm|all file(*.*)|*.*",
                FilterIndex = 1,
                Title = "Choose .pmm"
            };
            if (ofd.ShowDialog() == true)
            {
                await _model.OpenPmmAsync(File.ReadAllBytes(ofd.FileName));
            }
        }

        #endregion

        #region SavePmmCommand

        private ViewModelCommand _savePmmCommand;

        public ViewModelCommand SavePmmCommand => _savePmmCommand ?? (_savePmmCommand = new ViewModelCommand(SavePmm));

        private async void SavePmm()
        {
            var ofd = new SaveFileDialog
            {
                Filter = "pmm file(*.pmm)|*.pmm|json file(*.json)|*.json|zip file(*.zip)|*.zip",
                FilterIndex = 1
            };
            if (ofd.ShowDialog() == true)
            {
                string ext = Path.GetExtension(ofd.FileName);
                switch (ext)
                {
                    case ".pmm":
                        await _model.SavePmm(ofd.FileName);
                        break;
                    case ".json":
                        await _model.SavePmmJson(ofd.FileName);
                        break;
                    case ".zip":
                        await _model.SavePmmJson(ofd.FileName, true);
                        break;
                    default:
                        MessageBox.Show("Matching extension does not exist.");
                        break;
                }
            }
        }

        #endregion

        #region AllTimelineTranslateCommand

        private ViewModelCommand _allTimelineTranslateCommand;

        public ViewModelCommand AllTimelineTranslateCommand =>
            _allTimelineTranslateCommand ?? (_allTimelineTranslateCommand = new ViewModelCommand(AllTimelineTranslate));

        private void AllTimelineTranslate()
        {
            AddPane(() => new TimelineTranslateViewModel(_model));
        }

        #endregion

        #region OpenCameraLightAccessoryTimelineCommand

        private ViewModelCommand _openCameraLightAccessoryTimelineCommand;

        public ViewModelCommand OpenCameraLightAccessoryTimelineCommand =>
            _openCameraLightAccessoryTimelineCommand
            ?? (_openCameraLightAccessoryTimelineCommand =
                new ViewModelCommand(OpenCameraLightAccessoryTimeline));

        public async void OpenCameraLightAccessoryTimeline()
        {
            await AddDocument(
                () => Task.Run(()=> new CameraLightAccessoryViewModel(_model).AddTo(CompositeDisposables)),
                CameraLightAccessoryViewModel.GetContentId());
        }

        #endregion

        #region AvalonDock

        private async Task<T> AddDocument<T>(Func<Task<T>> createFunc, string contentId) where T : DocumentViewModelBase
        {
            DocumentViewModelBase item =
                DockingDocumentViewModels.FirstOrDefault(d => d.ContentId == contentId);
            if (item == null)
            {
                item = await createFunc();
                DockingDocumentViewModels.Add(item);
            }

            item.IsSelected = true;
            return (T) item;
        }

        private T AddPane<T>(Func<T> createFunc) where T : PaneViewModelBase
        {
            PaneViewModelBase item =
                DockingPaneViewModels.FirstOrDefault(d => d.ContentId == typeof(T).FullName);
            if (item == null)
            {
                item = createFunc();
                DockingPaneViewModels.Add(item);
            }

            item.Visibility = Visibility.Visible;
            item.IsSelected = true;
            item.IsActive = true;
            return (T) item;
        }

        public ObservableCollection<DocumentViewModelBase> DockingDocumentViewModels { get; } =
            new ObservableCollection<DocumentViewModelBase>();

        public ObservableCollection<PaneViewModelBase> DockingPaneViewModels { get; } =
            new ObservableCollection<PaneViewModelBase>();

        #endregion

        #region NextFrameCommand

        public ViewModelCommand NextFrameCommand { get; }

        private void NextFrame()
        {
            _model.FrameControlModel.NextFrame();
        }

        #endregion

        #region PrevFrameCommand

        public ViewModelCommand PrevFrameCommand { get; }

        private void PrevFrame()
        {
            _model.FrameControlModel.PrevFrame();
        }

        #endregion

        #region SwitchPlayAndStopCommand

        public ViewModelCommand SwitchPlayAndStopCommand { get; }

        private void SwitchPlayAndStop()
        {
            _model.FrameControlModel.SwitchPlayAndStop();
        }

        #endregion
    }
}
