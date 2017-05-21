using System;
using Livet.Commands;
using PMMEditor.Models;
using PMMEditor.Models.Graphics;
using PMMEditor.SharpDxControl;
using PMMEditor.ViewModels.Documents;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using SharpDX.Direct3D11;

namespace PMMEditor.ViewModels.Graphics
{
    public class MainRenderViewModel : DocumentViewModelBase
    {
        public CameraControlViewModel CameraControl { get; }

        public MainRenderViewModel(Model model)
        {
            CameraControl = new CameraControlViewModel(model.Camera);
            Device = GraphicsModel.Device;
            NowFrame = model.FrameControlModel.ObserveProperty(_ => _.NowFrame).ToReadOnlyReactiveProperty()
                            .AddTo(CompositeDisposables);
            Items = model.MmdModelList.List.ToReadOnlyReactiveCollection(_ =>
            {
                MmdModelRenderer renderer = _.GetComponent<MmdModelRenderer>();
                renderer.Initialize(model);
                return (IRenderer) renderer;
            }, UIDispatcherScheduler.Default)
                         .AddTo(CompositeDisposables);
            LookAtXResetCommand = new ListenerCommand<float>(_ => CameraControl.LookAtX = _);
            LookAtYResetCommand = new ListenerCommand<float>(_ => CameraControl.LookAtY = _);
            LookAtZResetCommand = new ListenerCommand<float>(_ => CameraControl.LookAtZ = _);

            RotateXResetCommand = new ListenerCommand<float>(_ => CameraControl.RotateX = _);
            RotateYResetCommand = new ListenerCommand<float>(_ => CameraControl.RotateY = _);
            RotateZResetCommand = new ListenerCommand<float>(_ => CameraControl.RotateZ = _);
            DistanceResetCommand = new ListenerCommand<float>(_ => CameraControl.Distance.Value = _);
        }

        public ReadOnlyReactiveProperty<int> NowFrame { get; }

        public void Initialize() { }

        public static string GetTitle() => "Main Camera";
        public static string GetContentId() => typeof(MainRenderViewModel).FullName + GetTitle();

        public override string Title { get; } = GetTitle();

        public override string ContentId { get; } = GetContentId();

        public ReadOnlyReactiveCollection<IRenderer> Items { get; set; }

        public Device Device { get; }

        public RenderTextureQueue RenderTextureQueue => Model.System.RenderTextureQueue;

        public ListenerCommand<float> LookAtXResetCommand { get; }

        public ListenerCommand<float> LookAtYResetCommand { get; }

        public ListenerCommand<float> LookAtZResetCommand { get; }

        public ListenerCommand<float> RotateXResetCommand { get; }

        public ListenerCommand<float> RotateYResetCommand { get; }

        public ListenerCommand<float> RotateZResetCommand { get; }

        public ListenerCommand<float> DistanceResetCommand { get; }
    }
}
