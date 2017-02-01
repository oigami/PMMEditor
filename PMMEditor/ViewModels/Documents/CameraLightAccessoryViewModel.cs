using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using PMMEditor.Models;

namespace PMMEditor.ViewModels.Documents
{
    public class CameraLightAccessoryViewModel : TimelineViewModelBase
    {

        public CameraLightAccessoryViewModel(Model model) : base(model)
        {
            CreateKeyFrame();
        }

        private void CreateKeyFrame()
        {
            ListOfKeyFrameList = new ObservableCollection<KeyFrameList>
                {
                    new KeyFrameList
                    {
                        Name = "Camera",
                        Frame = CreateList(_model.PmmStruct.CameraKeyFrames, _model.PmmStruct.CameraInitFrame)
                    },
                    new KeyFrameList
                    {
                        Name = "Light",
                        Frame = CreateList(_model.PmmStruct.LightKeyFrames, _model.PmmStruct.LightInitFrame)
                    },
                    new KeyFrameList
                    {
                        Name = "Self Shadow",
                        Frame = CreateList(_model.PmmStruct.SelfShadowKeyFrames, _model.PmmStruct.SelfShadowInitFrame)
                    },
                    new KeyFrameList
                    {
                        Name = "Gravity",
                        Frame = CreateList(_model.PmmStruct.GravityKeyFrames, _model.PmmStruct.GravityInitFrame)
                    }
                };


            var accessoryData = _model.PmmStruct.AccessoryDatas;
            foreach (PmmStruct.AccessoryData item in accessoryData)
            {
                ListOfKeyFrameList.Add(new KeyFrameList
                {
                    Name = item.Name,
                    Frame = CreateList(item.KeyFrames, item.InitFrame)
                });
            }
        }

        public static string GetTitle() => "Camera, Light, Accessory Timeline";
        public static string GetContentId() => typeof(CameraLightAccessoryViewModel).FullName + GetTitle();

        public override string Title { get; } = GetTitle();

        public override string ContentId { get; } = typeof(CameraLightAccessoryViewModel).FullName + GetTitle();
    }
}
