﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Livet;
using PMMEditor.Models;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Microsoft.Win32;
using PMMEditor.ViewModels.Panes;

namespace PMMEditor.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        /* コマンド、プロパティの定義にはそれぞれ
         *
         *  lvcom   : ViewModelCommand
         *  lvcomn  : ViewModelCommand(CanExecute無)
         *  llcom   : ListenerCommand(パラメータ有のコマンド)
         *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
         *  lprop   : 変更通知プロパティ(.NET4.5ではlpropn)
         *
         * を使用してください。
         *
         * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
         * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
         * LivetCallMethodActionなどから直接メソッドを呼び出してください。
         *
         * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
         * 同様に直接ViewModelのメソッドを呼び出し可能です。
         */

        /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
         * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
         */

        /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
         * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
         * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
         *
         * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
         * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
         *
         * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
         * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
         *
         * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
         */

        /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
         * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
         *
         * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
         * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
         */
        private readonly Model _model = new Model();
        private PropertyChangedEventListener _listener;

        public void Initialize()
        {
            _listener = new PropertyChangedEventListener(_model)
            {
                nameof(_model.PmmStruct),
                (_, __) =>
                {
                    RaisePropertyChanged(nameof(ModelList));
                    RaisePropertyChanged(nameof(PmmStruct));
                }
            };

#if DEBUG
            try
            {
                _model.OpenPmm(File.ReadAllBytes("C:/tool/MikuMikuDance_v926x64/UserFile/サンプル（きしめんAllStar).pmm"));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n" + e.StackTrace);
            }
#endif

            AddPane(() => new HeaderViewModel(_model));
        }

        #region ModelTab

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

        #endregion ModelTab

        #region AccessoryTab

        #region SelectedAccessory変更通知プロパティ

        private PmmStruct.AccessoryData _SelectedAccessory;

        public PmmStruct.AccessoryData SelectedAccessory
        {
            get { return _SelectedAccessory; }
            set
            {
                if (_SelectedAccessory == value)
                {
                    return;
                }
                _SelectedAccessory = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #endregion

        #region PmmStruct変更通知プロパティ

        public PmmStruct PmmStruct => _model.PmmStruct;

        #endregion

        #region OpenPmmCommand

        private ViewModelCommand _OpenPmmCommand;

        public ViewModelCommand OpenPmmCommand => _OpenPmmCommand ?? (_OpenPmmCommand = new ViewModelCommand(OpenPmm));

        private void OpenPmm()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "pmm file(*.pmm)|*.pmm|all file(*.*)|*.*",
                FilterIndex = 1,
                Title = "Choose .pmm"
            };
            if (ofd.ShowDialog() == true)
            {
                _model.OpenPmm(File.ReadAllBytes(ofd.FileName));
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

        #region AvalonDock

        private void AddPane<T>(Func<T> createFunc) where T : PaneViewModelBase
        {
            var item =
                DockingPaneViewModels.FirstOrDefault(d => d.ContentId == typeof(T).FullName);
            if (item == null)
            {
                item = createFunc();
            }
            else
            {
                DockingPaneViewModels.Remove(item);
            }
            DockingPaneViewModels.Add(item);
            item.IsSelected = true;
            item.IsActive = true;
        }

        public ObservableCollection<PaneViewModelBase> DockingDocumentViewModels { get; } =
            new ObservableCollection<PaneViewModelBase>();

        public ObservableCollection<PaneViewModelBase> DockingPaneViewModels { get; } =
            new ObservableCollection<PaneViewModelBase>();

        #endregion
    }
}
