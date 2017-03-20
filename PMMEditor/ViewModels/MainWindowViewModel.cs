using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PMMEditor.Log;
using PMMEditor.Models;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels
{
    internal class MainWindowViewModel : BindableDisposableBase
    {
        private readonly Model _model;
        private readonly LogMessageNotifier _logger;
        public MainWindowViewModel()
        {
            ReactivePropertyScheduler.SetDefault(CurrentThreadScheduler.Instance);
            _logger = new LogMessageNotifier();
            _model = new Model(_logger).AddTo(CompositeDisposable);
            WindowViewModel = new MMD.MainViewViewModel(_model, _logger)
                .AddTo(CompositeDisposable);
        }

        public async void Initialize()
        {
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
        }

        public object WindowViewModel { get; }
    }
}
