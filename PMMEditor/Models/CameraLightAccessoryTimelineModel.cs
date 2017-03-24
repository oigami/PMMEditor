using System;
using System.Linq;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Models
{
    internal class CameraLightAccessoryTimelineModel : BindableDisposableBase
    {
        public ReadOnlyReactiveCollection<KeyFrameList<MmdAccessoryModel.BoneKeyFrame,
            DefaultKeyFrameInterpolationMethod<MmdAccessoryModel.BoneKeyFrame>>> AccessoryKeyFrameLists { get; }

        public
            ReadOnlyReactiveCollection
                <KeyFrameList<CameraControlModel.BoneKeyFrame, CameraControlModel.BoneInterpolationMethod>>
            CamerakeyFrameLists { get; }

        public ReadOnlyReactiveCollection<KeyFrameList<MmdLightModel.BoneKeyFrame,
            DefaultKeyFrameInterpolationMethod<MmdLightModel.BoneKeyFrame>>> LightkeyFrameLists { get; }

        private void MaxFrameEventSubscribe<T, Method>(ReadOnlyReactiveCollection<KeyFrameList<T, Method>> list)
            where T : KeyFrameBase
            where Method : IKeyFrameInterpolationMethod<T>, new()
        {
            list.ObserveElementProperty(i => i.MaxFrame)
                .Subscribe(i => MaxFrameIndex = Math.Max(MaxFrameIndex, i.Value)).AddTo(CompositeDisposables);
            foreach (var item in list)
            {
                MaxFrameIndex = Math.Max(MaxFrameIndex, item.MaxFrame);
            }

            list.ObserveAddChanged()
                .Subscribe(item =>
                {
                    item.ObserveProperty(i => i.MaxFrame).Subscribe(i => MaxFrameIndex = Math.Max(MaxFrameIndex, i))
                        .AddTo(CompositeDisposables);
                    foreach (var boneKeyFrame in item)
                    {
                        MaxFrameIndex = Math.Max(boneKeyFrame.Key, MaxFrameIndex);
                    }
                }).AddTo(CompositeDisposables);
        }

        public CameraLightAccessoryTimelineModel(Model model)
        {
            AccessoryKeyFrameLists =
                model.MmdAccessoryList.List.ToReadOnlyReactiveCollection(i => i.BoneKeyList[0])
                     .AddTo(CompositeDisposables);
            MaxFrameEventSubscribe(AccessoryKeyFrameLists);

            CamerakeyFrameLists = model.Camera.BoneKeyList.ToReadOnlyReactiveCollection();
            MaxFrameEventSubscribe(CamerakeyFrameLists);

            LightkeyFrameLists = model.Light.BoneKeyList.ToReadOnlyReactiveCollection();
            MaxFrameEventSubscribe(LightkeyFrameLists);
        }

        private static bool CanMove<T, Method>(int diffFrame, ReadOnlyReactiveCollection<KeyFrameList<T, Method>> list)
            where T : KeyFrameBase
            where Method : IKeyFrameInterpolationMethod<T>, new()
        {
            return list.All(x => x.CanSelectedFrameMove(diffFrame));
        }

        private static void Move<T, Method>(int diffFrame, ReadOnlyReactiveCollection<KeyFrameList<T, Method>> list)
            where T : KeyFrameBase
            where Method : IKeyFrameInterpolationMethod<T>, new()
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

        private int _maxFrameIndex;

        public int MaxFrameIndex
        {
            get { return _maxFrameIndex; }
            set { SetProperty(ref _maxFrameIndex, value); }
        }

        #endregion
    }
}
