using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Livet.Commands;
using PMMEditor.Models;
using PMMEditor.Views.Timeline;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Documents
{
    public class CameraLightAccessoryViewModel : TimelineViewModelBase
    {
        private readonly CameraLightAccessoryTimelineModel _timelineModel;
        private readonly ReadOnlyReactiveCollection<TimelineKeyFrameList> _cameraKeyList;
        private readonly ReadOnlyReactiveCollection<TimelineKeyFrameList> _lightKeyList;

        public CameraLightAccessoryViewModel(Model model) : base(model)
        {
            _timelineModel = new CameraLightAccessoryTimelineModel(model).AddTo(CompositeDisposables);

            _cameraKeyList =
                _timelineModel.CamerakeyFrameLists.ToReadOnlyReactiveCollection(
                    i => TimelineKeyFrameList.Create(i, "Camera")).AddTo(CompositeDisposables);

            _lightKeyList =
                _timelineModel.LightkeyFrameLists.ToReadOnlyReactiveCollection(
                    i => TimelineKeyFrameList.Create(i, "Light")).AddTo(CompositeDisposables);

            MaxFrameIndex =
                _timelineModel.ObserveProperty(m => m.MaxFrameIndex).ToReactiveProperty().AddTo(CompositeDisposables);
            MaxFrameIndex.Subscribe(i => GridFrameNumberList.Resize(i)).AddTo(CompositeDisposables);

            KeyFrameMoveDeltaCommand = new ListenerCommand<KeyFrameMoveEventArgs>(
                args => _timelineModel.Move(args.DiffFrame));
            ListOfKeyFrameList = ReadOnlyMutliCollection.Merge(
               _cameraKeyList,
               _lightKeyList,
               _timelineModel.AccessoryKeyFrameLists
                             .ToReadOnlyReactiveCollection(TimelineKeyFrameList.Create)).AddTo(CompositeDisposables);
        }

        public static string GetTitle() => "Camera, Light, Accessory Timeline";
        public static string GetContentId() => typeof(CameraLightAccessoryViewModel).FullName + GetTitle();

        public override string Title { get; } = GetTitle();

        public override string ContentId { get; } = typeof(CameraLightAccessoryViewModel).FullName + GetTitle();

        public override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveStartedCommand { get; protected set; }

        public sealed override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveDeltaCommand { get; protected set; }

        public override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveCompletedCommand { get; protected set; }
    }
}
