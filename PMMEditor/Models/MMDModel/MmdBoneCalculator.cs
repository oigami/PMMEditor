using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.MMDFileParser;
using PMMEditor.MVVM;
using SharpDX;

namespace PMMEditor.Models.MMDModel
{
    public class MmdModelBoneCalculator : BindableBase
    {
        public MmdModelBoneCalculator(MmdModelModel model)
        {
            _model = model;
        }

        private readonly MmdModelModel _model;

        private void CalcBoneWorld(
            MmdModelModel.Bone me, Matrix parent,
            ref Matrix[] resultWorlds)
        {
            while (true)
            {
                var m = me.boneMat * parent;
                resultWorlds[me.id] = me.offsetMat * m;
                if (me.firstChild != -1)
                {
                    CalcBoneWorld(_model.BoneKeyList[me.firstChild], m, ref resultWorlds);
                }
                if (me.sibling != -1)
                {
                    me = _model.BoneKeyList[me.sibling];
                    continue;
                }
                break;
            }
        }

        public void Update()
        {
            var bones = _model.BoneKeyList;
            foreach (var i in Enumerable.Range(0, bones.Count))
            {
                var boneKeyFrames = _model.BoneKeyList.First(_ => _.name == bones[i].name);

                var pos = boneKeyFrames.KeyFrameList[0].Position;
                var q = boneKeyFrames.KeyFrameList[0].Quaternion;
                bones[i].boneMat = Matrix.RotationQuaternion(new Quaternion(q.X, q.Y, q.Z, q.W))
                                   * Matrix.Translation(pos.X, pos.Y, pos.Z) * bones[i].initMat;
            }
            WorldBones = Enumerable.Range(0, bones.Count).Select(_ => Matrix.Identity).ToArray();
            CalcBoneWorld(bones[0], Matrix.Identity, ref _worldBones);
            RaisePropertyChanged(nameof(WorldBones));
        }

        #region WorldBoneプロパティ

        private Matrix[] _worldBones;

        public Matrix[] WorldBones
        {
            get { return _worldBones; }
            set { SetProperty(ref _worldBones, value); }
        }

        #endregion

        public void InitBoneCalc()
        {
            InitBoneCalc(_model.BoneKeyList[0], Matrix.Identity);
            foreach (var i in _model.BoneKeyList)
            {
                i.boneMat = i.initMat;
            }
        }

        private void InitBoneCalc(MmdModelModel.Bone me, Matrix parentoffsetMat)
        {
            if (me.firstChild != -1)
            {
                InitBoneCalc(_model.BoneKeyList[me.firstChild], me.offsetMat);
            }
            if (me.sibling != -1)
            {
                InitBoneCalc(_model.BoneKeyList[me.sibling], parentoffsetMat);
            }
            me.initMat = me.initMatML * parentoffsetMat;
        }
    }
}
