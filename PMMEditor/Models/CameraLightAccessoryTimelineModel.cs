using System;
using System.Linq;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Models
{
    internal class CameraLightAccessoryTimelineModel : BindableDisposableBase
    {
        public ReadOnlyReactiveCollection<KeyFrameList<MmdAccessoryModel.BoneKeyFrame>> AccessoryKeyFrameLists { get; }

        public ReadOnlyReactiveCollection<KeyFrameList<MmdCameraModel.BoneKeyFrame>> CamerakeyFrameLists { get; }

        public ReadOnlyReactiveCollection<KeyFrameList<MmdLightModel.BoneKeyFrame>> LightkeyFrameLists { get; }

        private void MaxFrameEventSubscribe<T>(ReadOnlyReactiveCollection<KeyFrameList<T>> list) where T : KeyFrameBase
        {
            list.ObserveElementProperty(i => i.MaxFrame)
                .Subscribe(i => MaxFrameIndex = Math.Max(MaxFrameIndex, i.Value)).AddTo(CompositeDisposable);
            foreach (var item in list)
            {
                MaxFrameIndex = Math.Max(MaxFrameIndex, item.MaxFrame);
            }

            list.ObserveAddChanged()
                .Subscribe(item =>
                {
                    item.ObserveProperty(i => i.MaxFrame).Subscribe(i => MaxFrameIndex = Math.Max(MaxFrameIndex, i))
                        .AddTo(CompositeDisposable);
                    foreach (var boneKeyFrame in item)
                    {
                        MaxFrameIndex = Math.Max(boneKeyFrame.Key, MaxFrameIndex);
                    }
                }).AddTo(CompositeDisposable);
        }

        public CameraLightAccessoryTimelineModel(Model model)
        {
            AccessoryKeyFrameLists =
                model.MmdAccessoryList.List.ToReadOnlyReactiveCollection(i => i.BoneKeyList[0])
                     .AddTo(CompositeDisposable);
            MaxFrameEventSubscribe(AccessoryKeyFrameLists);

            CamerakeyFrameLists = model.Camera.BoneKeyList.ToReadOnlyReactiveCollection();
            MaxFrameEventSubscribe(CamerakeyFrameLists);

            LightkeyFrameLists = model.Light.BoneKeyList.ToReadOnlyReactiveCollection();
            MaxFrameEventSubscribe(LightkeyFrameLists);
        }

        private static bool CanMove<T>(int diffFrame, ReadOnlyReactiveCollection<KeyFrameList<T>> list)
            where T : KeyFrameBase
        {
            return list.All(x => x.CanSelectedFrameMove(diffFrame));
        }

        private static void Move<T>(int diffFrame, ReadOnlyReactiveCollection<KeyFrameList<T>> list)
            where T : KeyFrameBase
        {
            foreach (var item in list)
            {
                item.SelectedFrameMove(diffFrame);
            }
        }

        public void Move(int diffFrame)
        {
            if (CanMove(diffFrame, AccessoryKeyFrameLists) == false ||
                CanMove(diffFrame, CamerakeyFrameLists) == false ||
                CanMove(diffFrame, LightkeyFrameLists) == false)
            {
                return;
            }

            Move(diffFrame, AccessoryKeyFrameLists);
            Move(diffFrame, CamerakeyFrameLists);
            Move(diffFrame, LightkeyFrameLists);
        }

        #region MaxFrameIndex変更通知プロパティ

        private int _MaxFrameIndex;

        public int MaxFrameIndex
        {
            get { return _MaxFrameIndex; }
            set { SetProperty(ref _MaxFrameIndex, value); }
        }

        #endregion
    }
}
