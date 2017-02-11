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

namespace PMMEditor.ViewModels.Documents
{
    public class CameraLightAccessoryViewModel : TimelineViewModelBase
    {
        public CameraLightAccessoryViewModel(Model model) : base(model) {}

        public async Task Initialize()
        {
            ListOfKeyFrameList = new ObservableCollection<TimelineKeyFrameList>(await Task.Run(() =>
            {
                var list = new List<TimelineKeyFrameList>
                {
                    new TimelineKeyFrameList
                    {
                        Name = "Camera",
                        Frame = CreateList(_model.PmmStruct.CameraKeyFrames, _model.PmmStruct.CameraInitFrame)
                    },
                    new TimelineKeyFrameList
                    {
                        Name = "Light",
                        Frame = CreateList(_model.PmmStruct.LightKeyFrames, _model.PmmStruct.LightInitFrame)
                    },
                    new TimelineKeyFrameList
                    {
                        Name = "Self Shadow",
                        Frame = CreateList(_model.PmmStruct.SelfShadowKeyFrames, _model.PmmStruct.SelfShadowInitFrame)
                    },
                    new TimelineKeyFrameList
                    {
                        Name = "Gravity",
                        Frame = CreateList(_model.PmmStruct.GravityKeyFrames, _model.PmmStruct.GravityInitFrame)
                    }
                };

                var accessoryData = _model.MmdAccessoryList.List;
                Func<KeyFrameList<MmdAccessoryModel.BoneKeyFrame>, List<TimelineFrameData>>
                    createAccessoryFrame = item =>
                    {
                        var res = new List<TimelineFrameData>();
                        foreach (var frame in item)
                        {
                            res.Add(new TimelineFrameData(frame.Key, frame.Value.IsSelected));
                        }
                        return res;
                    };
                foreach (var item in accessoryData)
                {
                    list.Add(new TimelineKeyFrameList
                    {
                        Name = item.Name,
                        Frame = createAccessoryFrame(item.BoneKeyList[0])
                    });
                }
                return list;
            }));
        }

        public static string GetTitle() => "Camera, Light, Accessory Timeline";
        public static string GetContentId() => typeof(CameraLightAccessoryViewModel).FullName + GetTitle();

        public override string Title { get; } = GetTitle();

        public override string ContentId { get; } = typeof(CameraLightAccessoryViewModel).FullName + GetTitle();

        public override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveStartedCommand { get; protected set; }

        public override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveDeltaCommand { get; protected set; }

        public override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveCompletedCommand { get; protected set; }
    }
}
