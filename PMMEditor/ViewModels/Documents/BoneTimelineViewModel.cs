using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using Livet.Commands;
using PMMEditor.Models;
using PMMEditor.Models.MMDModel;
using PMMEditor.Views.Timeline;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Documents
{
    public class BoneTimelineViewModel : TimelineViewModelBase
    {
        public MmdModelModel Model { get; }

        public BoneTimelineViewModel(Model model, MmdModelModel modelModel) : base(model)
        {
            Model = modelModel;
            ListOfKeyFrameList = modelModel.BoneKeyList.ToReadOnlyReactiveCollection(
                _ => TimelineKeyFrameList.Create(_.KeyFrameList)).AddTo(CompositeDisposables);

            Title = modelModel.Name;
            ContentId = typeof(CameraLightAccessoryViewModel).FullName + modelModel.NameEnglish;
        }

        public override string Title { get; }

        public override string ContentId { get; }

        public override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveStartedCommand { get; protected set; }

        public sealed override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveDeltaCommand { get; protected set; }

        public override ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveCompletedCommand { get; protected set; }
    }
}
