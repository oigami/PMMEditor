using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Livet.Commands;
using PMMEditor.Models;
using PMMEditor.Views.Documents;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

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
            ListOfKeyFrameList
                = Enumerable
                    .Range(0, 3)
                    .Select(i => new TimelineKeyFrameList {Name = "test" + i})
                    .Concat(_timelineModel.AccessoryKeyFrameLists.Select(
                        TimelineKeyFrameList.Create))
                    .ToReadOnlyReactiveCollection(
                        _timelineModel
                            .AccessoryKeyFrameLists
                            .ToCollectionChanged()
                            .SelectMany(i =>
                            {
                                var res = new CollectionChanged<TimelineKeyFrameList>
                                {
                                    Action = i.Action,
                                    Index = i.Index + 3,
                                    OldIndex = i.OldIndex + 3,
                                    Value = i.Value != null
                                        ? TimelineKeyFrameList.Create(i.Value)
                                        : null
                                };
                                return
                                    Enumerable.Range(0, 1)
                                              .Select(_ => res)
                                              .Concat(res.Action == NotifyCollectionChangedAction.Reset
                                                  ? Enumerable.Range(0, 3)
                                                              .Select(
                                                                  x => CollectionChanged
                                                                      <TimelineKeyFrameList>.Add(
                                                                          x,
                                                                          new TimelineKeyFrameList
                                                                          {
                                                                              Name = "test" + x
                                                                          }))
                                                  : Enumerable.Empty<CollectionChanged<TimelineKeyFrameList>>());
                            }))
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
