using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            _modelLocalBones2 = Enumerable.Range(0, bones.Count).Select(_ => Matrix.Identity).ToArray();
            _worldBones = Enumerable.Range(0, bones.Count).Select(_ => Matrix.Identity).ToArray();
        }

        private readonly MmdModelModel _model;

        private void CalcBoneWorld(
            MmdModelModel.Bone me, Matrix parent,
            ref Matrix[] inOutWorlds)
        {
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            foreach (var bone in bones)
            {
                inOutWorlds[bone.Index] = bone.InverseInitMatModelLocal * inOutWorlds[bone.Index];
            }
        }

        private void CalcBoneModelLocalMatrix(
            MmdModelModel.Bone me, Matrix parent,
            ref Matrix[] resultModelLocalMatrix)
        {
            while (true)
            {
                Matrix m = me.BoneMatBoneLocal * parent;
                resultModelLocalMatrix[me.Index] = m;
                if (me.FirstChildIndex != -1)
                {
                    CalcBoneModelLocalMatrix(_model.BoneKeyList[me.FirstChildIndex], m, ref resultModelLocalMatrix);
                }
                if (me.SiblingIndex == -1)
                {
                    break;
                }

                me = _model.BoneKeyList[me.SiblingIndex];
            }
        }


        public void Update(IList<MmdModelModel.BoneKeyFrame> nowBoneKeyFrame)
        {
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            UpdateForwardKinematics(nowBoneKeyFrame);
            UpdateInverseKinematics();
            CalcBoneModelLocalMatrix(bones[0], Matrix.Identity, ref _modelLocalBones2);
            ModelLocalBones.CopyTo(WorldBones, 0);
            CalcBoneWorld(bones[0], Matrix.Identity, ref _worldBones);
        }

        private void UpdateForwardKinematics(IList<MmdModelModel.BoneKeyFrame> nowBoneKeyFrame)
        {
            Debug.Assert(_model.BoneKeyList.Count == nowBoneKeyFrame.Count);
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            foreach (var i in Enumerable.Range(0, bones.Count))
            {

                MmdModelModel.BoneKeyFrame data = nowBoneKeyFrame[i];
                System.Numerics.Vector3 pos = data.Position;
                System.Numerics.Quaternion q = data.Quaternion;

                bones[i].BoneMatBoneLocal = Matrix.RotationQuaternion(new Quaternion(q.X, q.Y, q.Z, q.W))
                                            * Matrix.Translation(pos.X, pos.Y, pos.Z) * bones[i].InitMatBoneLocal;
            }
        }

        private void UpdateInverseKinematics()
        {
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            foreach (var ik in _model.IKList)
            {
                Matrix targetMatrix = CalcBoneModelLocalMatrix(ik.BoneIndex);
                foreach (var i in Enumerable.Range(0, ik.Iterations))
                {
                    foreach (var attentionIndex in ik.IKChildBoneIndex)
                    {
                        MmdModelModel.Bone bone = bones[attentionIndex];

                        Matrix effectorMatrix = CalcBoneModelLocalMatrix(ik.TargetBoneIndex);

                        Matrix inverseCoord = Matrix.Invert(CalcBoneModelLocalMatrix(attentionIndex));

                        Vector3 localEffectorDir = Vector3.TransformCoordinate(effectorMatrix.TranslationVector,
                                                                           inverseCoord);
                        Vector3 localTargetDir = Vector3.TransformCoordinate(targetMatrix.TranslationVector,
                                                                         inverseCoord);

                        localEffectorDir.Normalize();
                        localTargetDir.Normalize();
                        float cosAngle = Vector3.Dot(localEffectorDir, localTargetDir);
                        if (1.0f <= cosAngle)
                        {
                            continue;
                        }

                        float angle =
                            Math.Min((float) Math.Acos(Math.Max(cosAngle, -1.0)), ik.LimitAngle * (float) Math.PI);
                        Debug.Assert(float.IsNaN(angle) == false && float.IsInfinity(angle) == false);
                        if (Math.Abs(angle) < 1e-7f)
                        {
                            continue;
                        }

                        Vector3 axis = Vector3.Cross(localEffectorDir, localTargetDir);
                        if (bone.Name == "左足" || bone.Name == "右足")
                        {
                            axis.Y = 0.0f;
                        }

                        if (axis.IsZero)
                        {
                            continue;
                        }

                        axis.Normalize();

                        Quaternion rotation = Quaternion.RotationAxis(axis, angle);
                        if (bone.Name == "左ひざ" || bone.Name == "右ひざ")
                        {
                            Quaternion rv = rotation * Quaternion.RotationMatrix(bone.BoneMatBoneLocal);
                            rv.Normalize();
                            var eulerAngle = new EulerAngles(Matrix.RotationQuaternion(rv));
                            eulerAngle.X = Clamp(eulerAngle.X, MathUtil.Radians(-180.0f), MathUtil.Radians(-10.0f));
                            eulerAngle.Y = 0;
                            eulerAngle.Z = 0;
                            bone.BoneMatBoneLocal = eulerAngle.CreateMatrix()
                                                    * Matrix.Translation(bone.BoneMatBoneLocal.TranslationVector);
                        }
                        else
                        {
                            bone.BoneMatBoneLocal = Matrix.RotationQuaternion(rotation) * bone.BoneMatBoneLocal;
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
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            Matrix res = bones[index].BoneMatBoneLocal;

            for (int parent = bones[index].ParentIndex; parent != -1; parent = bones[parent].ParentIndex)
            {
                res *= bones[parent].BoneMatBoneLocal;
            }

            return res;
        }

        #region WorldBoneプロパティ

        private Matrix[] _worldBones;

        public Matrix[] WorldBones => _worldBones;

        #endregion

        #region ModelLocalBonesプロパティ

        private Matrix[] _modelLocalBones2;

        public Matrix[] ModelLocalBones => _modelLocalBones2;

        #endregion

        public void InitBoneCalc()
        {
            InitBoneCalc(_model.BoneKeyList[0], Matrix.Identity);
            foreach (var i in _model.BoneKeyList)
            {
                i.BoneMatBoneLocal = i.InitMatBoneLocal;
            }
        }

        private void InitBoneCalc(MmdModelModel.Bone me, Matrix parentoffsetMat)
        {
            while (true)
            {
                me.InitMatBoneLocal = me.InitMatModelLocal * parentoffsetMat;
                if (me.SiblingIndex != -1)
                {
                    InitBoneCalc(_model.BoneKeyList[me.SiblingIndex], parentoffsetMat);
                }
                if (me.FirstChildIndex == -1)
                {
                    break;
                }

                parentoffsetMat = me.InverseInitMatModelLocal;
                me = _model.BoneKeyList[me.FirstChildIndex];
            }
        }
    }
}
