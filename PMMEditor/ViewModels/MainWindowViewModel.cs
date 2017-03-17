using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PMMEditor.Models;
using PMMEditor.MVVM;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels
{
    internal class MainWindowViewModel : BindableDisposableBase
    {
        private readonly Model _model;
        public MainWindowViewModel()
        {
            _model = new Model().AddTo(CompositeDisposable);
            WindowViewModel = new MMD.MainViewViewModel(_model);
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
