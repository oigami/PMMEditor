using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.Models;
using PMMEditor.MVVM;
using PMMEditor.ViewModels.Documents;
using PMMEditor.ViewModels.Graphics;

namespace PMMEditor.ViewModels.MMD
{
    internal class MainViewViewModel : BindableDisposableBase
    {
        private readonly Model _model;

        public MainViewViewModel(Model model)
        {
            _model = model;
            TimelineViewModel = new CameraLightAccessoryViewModel(_model);
            RendererViewModel = new MainRenderViewModel(_model);
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
    }
}
