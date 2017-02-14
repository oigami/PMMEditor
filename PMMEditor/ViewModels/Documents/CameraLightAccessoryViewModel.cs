using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
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

namespace PMMEditor.ViewModels.Documents
{
    public class CameraLightAccessoryViewModel : TimelineViewModelBase
    {
        public CameraLightAccessoryViewModel(Model model) : base(model)
        {
            KeyFrameMoveDeltaCommand = new ListenerCommand<KeyFrameMoveEventArgs>(
                args =>
                {
                    var accessoryList = _model.MmdAccessoryList.List;
                    if (accessoryList.All(item => item.BoneKeyList[0].CanSelectedFrameMove(args.DiffFrame))) {
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
            ListOfKeyFrameList = new ObservableCollection<TimelineKeyFrameList>(await Task.Run(() =>
            {
                var list = new List<TimelineKeyFrameList>();

                var accessoryData = _model.MmdAccessoryList.List;
                list.AddRange(accessoryData.Select(item => new TimelineKeyFrameList
                {
                    Name = item.Name,
                    Frame =
                        item.BoneKeyList[0].Select(frame =>
                        {
                            var res = new TimelineFrameData(frame.Key, frame.Value.IsSelected);
                            res.ToReactivePropertyAsSynchronized(data => data.IsSelected)
                               .Subscribe(isSelected => item.BoneKeyList[0].Select(res.FrameNumber, isSelected));
                            frame.Value.MoveChanged += (index, diff) => { res.FrameNumber = index + diff; };
                            MaxFrameIndex = Math.Max(res.FrameNumber, MaxFrameIndex);
                            return res;
                        }).ToList()
                }));
                return list;
            }));
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
