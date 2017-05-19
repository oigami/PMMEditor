using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PMMEditor.ECS;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Models.MMDModel
{
    public class BoneFrameControlModel : Component
    {
        private ObservableCollection<MmdModelModel.Bone> _boneList;
        private readonly CompositeDisposable _compositeDisposables = new CompositeDisposable();

        public ObservableCollection<MmdModelModel.BoneKeyFrame> NowBoneKeyFrame { get; } =
            new ObservableCollection<MmdModelModel.BoneKeyFrame>();

        public MmdModelBoneCalculator BoneCalculator { get; private set; }

        public IFrameControlModel _frameControlModel;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _compositeDisposables.Dispose();
        }

        public BoneFrameControlModel Initialize()
        {
            _frameControlModel = GameObject.GetComponent<FrameControlFilter>().ControlModel;
            MmdModelModel model = GameObject.GetComponent<MmdModelModel>();
            _boneList = model.BoneKeyList;
            _compositeDisposables.Add(Disposable.Create(() => _boneList = null));

            BoneCalculator = new MmdModelBoneCalculator(model);
            BoneCalculator.InitBoneCalc();
            return this;
        }

        public override void Update()
        {
            ResizeList();
            foreach (var i in Enumerable.Range(0, _boneList.Count))
            {
                _boneList[i].KeyFrameList.GetInterpolationData(_frameControlModel.NowFrame).CopyTo(NowBoneKeyFrame[i]);
            }

            BoneCalculator.Update(NowBoneKeyFrame);
        }

        private void ResizeList()
        {
            foreach (var i in Enumerable.Range(0, Math.Max(0, _boneList.Count - NowBoneKeyFrame.Count)))
            {
                NowBoneKeyFrame.Add(new MmdModelModel.BoneKeyFrame());
            }
        }
    }
}
