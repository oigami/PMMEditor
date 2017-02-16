using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;
using PMMEditor.Models;
using PMMEditor.Views.Documents;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;

namespace PMMEditor.ViewModels.Documents
{
    public class CameraLightAccessoryViewModel : TimelineViewModelBase
    {
        private readonly CameraLightAccessoryTimelineModel _timelineModel;

        public CameraLightAccessoryViewModel(Model model) : base(model)
        {
            _timelineModel = new CameraLightAccessoryTimelineModel(model).AddTo(CompositeDisposable);
            MaxFrameIndex =
                _timelineModel.ObserveProperty(m => m.MaxFrameIndex).ToReactiveProperty().AddTo(CompositeDisposable);
            MaxFrameIndex.Subscribe(i => GridFrameNumberList.Resize(i)).AddTo(CompositeDisposable);
            KeyFrameMoveDeltaCommand = new ListenerCommand<KeyFrameMoveEventArgs>(
                args =>
                {
                    var accessoryList = _model.MmdAccessoryList.List;
                    if (accessoryList.All(item => item.BoneKeyList[0].CanSelectedFrameMove(args.DiffFrame)) == false)
                    {
                        return;
                    }
                    foreach (var item in accessoryList)
                    {
                        item.BoneKeyList[0].SelectedFrameMove(args.DiffFrame);
                    }
                });
        }

        public async Task Initialize()
        {
            ListOfKeyFrameList =
                _timelineModel.AccessoryKeyFrameLists
                      .ToReadOnlyReactiveCollection(TimelineKeyFrameList.Create)
                      .AddTo(CompositeDisposable);
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
