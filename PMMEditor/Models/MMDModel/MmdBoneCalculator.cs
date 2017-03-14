using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            ref Matrix[] inOutWorlds)
        {
            var bones = _model.BoneKeyList;
            foreach (var bone in bones)
            {
                inOutWorlds[bone.id] = bone.offsetMat * inOutWorlds[bone.id];
            }
        }

        private void CalcBoneModelLocalMatrix(
            MmdModelModel.Bone me, Matrix parent,
            ref Matrix[] resultModelLocalMatrix)
        {
            while (true)
            {
                var m = me.boneMat * parent;
                resultModelLocalMatrix[me.id] = m;
                if (me.firstChild != -1)
                {
                    CalcBoneModelLocalMatrix(_model.BoneKeyList[me.firstChild], m, ref resultModelLocalMatrix);
                }
                if (me.sibling != -1)
                {
                    me = _model.BoneKeyList[me.sibling];
                    continue;
                }
                break;
            }
        }


        public void Update(int frameIndex)
        {
            var bones = _model.BoneKeyList;
            UpdateForwardKinematics(frameIndex);
            UpdateInverseKinematics();
            ModelLocalBones = Enumerable.Range(0, bones.Count).Select(_ => Matrix.Identity).ToArray();
            WorldBones = Enumerable.Range(0, bones.Count).Select(_ => Matrix.Identity).ToArray();
            CalcBoneModelLocalMatrix(bones[0], Matrix.Identity, ref _ModelLocalBones);
            ModelLocalBones.CopyTo(WorldBones, 0);
            CalcBoneWorld(bones[0], Matrix.Identity, ref _worldBones);
            RaisePropertyChanged(nameof(WorldBones));
        }

        private void UpdateForwardKinematics(int index)
        {
            var bones = _model.BoneKeyList;
            foreach (var i in Enumerable.Range(0, bones.Count))
            {
                if (bones[i].type == PmdStruct.BoneKind.IKAffected
                    || bones[i].type == PmdStruct.BoneKind.IKTarget)
                {
                    continue;
                }

                var boneKeyFrames = _model.BoneKeyList.First(_ => _.name == bones[i].name);

                var data = boneKeyFrames.KeyFrameList.GetInterpolationData(index);
                var pos = data.Position;
                var q = data.Quaternion;

                bones[i].boneMat = Matrix.RotationQuaternion(new Quaternion(q.X, q.Y, q.Z, q.W))
                                   * Matrix.Translation(pos.X, pos.Y, pos.Z) * bones[i].initMat;
            }
        }

        private void UpdateInverseKinematics()
        {
            var bones = _model.BoneKeyList;
            foreach (var ik in _model.IKList)
            {
                var targetMatrix = CalcBoneModelLocalMatrix(ik.BoneIndex);
                foreach (var i in Enumerable.Range(0, ik.Iterations))
                {
                    foreach (var attentionIndex in ik.IKChildBoneIndex)
                    {
                        var bone = bones[attentionIndex];

                        var effectorMatrix = CalcBoneModelLocalMatrix(ik.TargetBoneIndex);

                        var inverseCoord = Matrix.Invert(CalcBoneModelLocalMatrix(attentionIndex));

                        var localEffectorDir = Vector3.TransformCoordinate(effectorMatrix.TranslationVector,
                                                                           inverseCoord);
                        var localTargetDir = Vector3.TransformCoordinate(targetMatrix.TranslationVector,
                                                                         inverseCoord);

                        localEffectorDir.Normalize();
                        localTargetDir.Normalize();

                        var angle =
                            Math.Min((float) Math.Acos(Clamp(Vector3.Dot(localEffectorDir, localTargetDir),
                                                             -1.0, 1.0f)),
                                     ik.LimitAngle * (float) Math.PI);
                        Debug.Assert(float.IsNaN(angle) == false && float.IsInfinity(angle) == false);
                        if (Math.Abs(angle) < 1e-7f)
                        {
                            continue;
                        }

                        var axis = Vector3.Cross(localEffectorDir, localTargetDir);
                        if (bone.name == "左足" || bone.name == "右足")
                        {
                            axis.Y = 0.0f;
                        }

                        if (axis.IsZero)
                        {
                            continue;
                        }
                        axis.Normalize();

                        var rotation = Quaternion.RotationAxis(axis, angle);
                        if (bone.name == "左ひざ" || bone.name == "右ひざ")
                        {
                            Quaternion rv = rotation * Quaternion.RotationMatrix(bone.boneMat);
                            rv.Normalize();
                            var eulerAngle = new EulerAngles(Matrix.RotationQuaternion(rv));
                            eulerAngle.X = Clamp(eulerAngle.X, MathUtil.Radians(-180.0f), MathUtil.Radians(-10.0f));
                            eulerAngle.Y = 0;
                            eulerAngle.Z = 0;
                            bone.boneMat = eulerAngle.CreateMatrix()
                                           * Matrix.Translation(bone.boneMat.TranslationVector);
                        }
                        else
                        {
                            bone.boneMat = Matrix.RotationQuaternion(rotation) * bone.boneMat;
                        }
                    }
                }
            }
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(max) > 0)
            {
                return max;
            }
            if (value.CompareTo(min) < 0)
            {
                return min;
            }
            return value;
        }

        private Matrix CalcBoneModelLocalMatrix(int index)
        {
            var bones = _model.BoneKeyList;
            Matrix res = bones[index].boneMat;

            for (int parent = bones[index].parent; parent != -1; parent = bones[parent].parent)
            {
                res *= bones[parent].boneMat;
            }

            return res;
        }

        #region WorldBoneプロパティ

        private Matrix[] _worldBones;

        public Matrix[] WorldBones
        {
            get { return _worldBones; }
            set { SetProperty(ref _worldBones, value); }
        }

        #endregion

        #region ModelLocalBonesプロパティ

        private Matrix[] _ModelLocalBones;

        public Matrix[] ModelLocalBones
        {
            get { return _ModelLocalBones; }
            set { SetProperty(ref _ModelLocalBones, value); }
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
