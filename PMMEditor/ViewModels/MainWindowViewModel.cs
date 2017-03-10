using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using PMMEditor.Models;
using Livet.Commands;
using Microsoft.Win32;
using PMMEditor.MMDFileParser;
using PMMEditor.MVVM;
using PMMEditor.ViewModels.Documents;
using PMMEditor.ViewModels.Graphics;
using PMMEditor.ViewModels.Panes;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels
{
    public class MainWindowViewModel : BindableDisposableBase
    {
        private readonly Model _model = new Model();

        public async void Initialize()
        {
            _model.ObserveProperty(_ => _.PmmStruct).Subscribe(_ => RaisePropertyChanged(nameof(PmmStruct)))
                  .AddTo(CompositeDisposable);
#if DEBUG
            try
            {
                await _model.OpenPmm(File.ReadAllBytes("C:/tool/MikuMikuDance_v926x64/UserFile/サンプル（きしめんAllStar).pmm"));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n" + e.StackTrace);
            }
#endif

            AddPane(() => new HeaderViewModel(_model).AddTo(CompositeDisposable));
            AddPane(() => new ModelViewModel(_model).AddTo(CompositeDisposable));
            AddPane(() => new CameraViewModel(_model).AddTo(CompositeDisposable));
            AddPane(() => new AccessoryViewModel(_model).AddTo(CompositeDisposable));

            await AddDocument(async () => new MainRenderViewModel(_model), "test");
        }

        #region PmmStruct変更通知プロパティ

        public PmmStruct PmmStruct => _model.PmmStruct;

        #endregion

        #region OpenPmmCommand

        private ViewModelCommand _OpenPmmCommand;

        public ViewModelCommand OpenPmmCommand => _OpenPmmCommand ?? (_OpenPmmCommand = new ViewModelCommand(OpenPmm));

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
                await _model.OpenPmm(File.ReadAllBytes(ofd.FileName));
            }
        }

        #endregion

        #region SavePmmCommand

        private ViewModelCommand _SavePmmCommand;

        public ViewModelCommand SavePmmCommand => _SavePmmCommand ?? (_SavePmmCommand = new ViewModelCommand(SavePmm));

        private async void SavePmm()
        {
            var ofd = new SaveFileDialog
            {
                Filter = "pmm file(*.pmm)|*.pmm|json file(*.json)|*.json|zip file(*.zip)|*.zip",
                FilterIndex = 1
            };
            if (ofd.ShowDialog() == true)
            {
                var ext = Path.GetExtension(ofd.FileName);
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

        private ViewModelCommand _AllTimelineTranslateCommand;

        public ViewModelCommand AllTimelineTranslateCommand =>
            _AllTimelineTranslateCommand ?? (_AllTimelineTranslateCommand = new ViewModelCommand(AllTimelineTranslate));

        private void AllTimelineTranslate()
        {
            AddPane(() => new TimelineTranslateViewModel(_model));
        }

        #endregion

        #region OpenCameraLightAccessoryTimelineCommand

        private ViewModelCommand _OpenCameraLightAccessoryTimelineCommand;

        public ViewModelCommand OpenCameraLightAccessoryTimelineCommand =>
            _OpenCameraLightAccessoryTimelineCommand
            ?? (_OpenCameraLightAccessoryTimelineCommand =
                new ViewModelCommand(OpenCameraLightAccessoryTimeline));

        public async void OpenCameraLightAccessoryTimeline()
        {
            await AddDocument(async () =>
            {
                var res = new CameraLightAccessoryViewModel(_model).AddTo(CompositeDisposable);
                await res.Initialize();
                return res;
            }, CameraLightAccessoryViewModel.GetContentId());
        }

        #endregion

        #region AvalonDock

        private async Task<T> AddDocument<T>(Func<Task<T>> createFunc, string contentId) where T : DocumentViewModelBase
        {
            var item =
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
            var item =
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
    }
}
