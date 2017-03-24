using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Models.MMDModel
{
    public class BoneFrameControlModel : BindableDisposableBase
    {
        private ObservableCollection<MmdModelModel.Bone> _boneList;

        public ObservableCollection<MmdModelModel.BoneKeyFrame> NowBoneKeyFrame { get; } =
            new ObservableCollection<MmdModelModel.BoneKeyFrame>();

        public MmdModelBoneCalculator BoneCalculator { get; }

        public BoneFrameControlModel(IFrameControlModel nowFrame, MmdModelModel model)
        {
            _boneList = model.BoneKeyList;
            CompositeDisposables.Add(Disposable.Create(() => _boneList = null));

            BoneCalculator = new MmdModelBoneCalculator(model);
            BoneCalculator.InitBoneCalc();

            nowFrame.ObserveProperty(_ => _.NowFrame).Subscribe(_ =>
            {
                ResizeList();
                foreach (var i in Enumerable.Range(0, _boneList.Count))
                {
                    _boneList[i].KeyFrameList.GetInterpolationData(_).CopyTo(NowBoneKeyFrame[i]);
                }
                BoneCalculator.Update(NowBoneKeyFrame);
            }).AddTo(CompositeDisposables);

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
