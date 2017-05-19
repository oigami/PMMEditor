using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.ECS;
using PMMEditor.MMDFileParser;
using PMMEditor.MVVM;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace PMMEditor.Models.MMDModel
{
    public class MmdModelBoneCalculator : Component
    {
        public MmdModelBoneCalculator(MmdModelModel model)
        {
            _model = model;
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            _modelLocalBones2 = Enumerable.Range(0, bones.Count).Select(_ => Matrix4x4.Identity).ToArray();
            _worldBones = Enumerable.Range(0, bones.Count).Select(_ => Matrix4x4.Identity).ToArray();
        }

        private readonly MmdModelModel _model;

        private void CalcBoneWorld(
            ref Matrix4x4[] inOutWorlds)
        {
            foreach (var bone in _model.BoneKeyList)
            {
                inOutWorlds[bone.Index] = bone.InverseInitMatModelLocal * inOutWorlds[bone.Index];
            }
        }

        private void CalcBoneModelLocalMatrix(
            MmdModelModel.Bone me, Matrix4x4 parent,
            ref Matrix4x4[] resultModelLocalMatrix)
        {
            while (true)
            {
                Matrix4x4 m = me.BoneMatBoneLocal * parent;
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
            CalcBoneModelLocalMatrix(bones[0], Matrix4x4.Identity, ref _modelLocalBones2);
            ModelLocalBones.CopyTo(WorldBones, 0);
            CalcBoneWorld(ref _worldBones);
        }

        private void UpdateForwardKinematics(IList<MmdModelModel.BoneKeyFrame> nowBoneKeyFrame)
        {
            Debug.Assert(_model.BoneKeyList.Count == nowBoneKeyFrame.Count);
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            foreach (var i in Enumerable.Range(0, bones.Count))
            {
                MmdModelModel.BoneKeyFrame data = nowBoneKeyFrame[i];
                Vector3 pos = data.Position;
                Quaternion q = data.Quaternion;

                bones[i].BoneMatBoneLocal = Matrix4x4.CreateFromQuaternion(new Quaternion(q.X, q.Y, q.Z, q.W))
                                            * Matrix4x4.CreateTranslation(pos.X, pos.Y, pos.Z) * bones[i].InitMatBoneLocal;
            }
        }

        private void UpdateInverseKinematics()
        {
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            foreach (var (ikBone, j) in _model.IKList)
            {
                PmxStruct.Bone.IKData ik = ikBone.IK;

                Vector3 targetMatrix = CalcBoneModelLocalPosition(j);
                foreach (var i in Enumerable.Range(0, ik.Iterations))
                {
                    foreach (var ikLink in ik.IKLinks)
                    {
                        int attentionIndex = ikLink.BoneIndex;
                        MmdModelModel.Bone bone = bones[attentionIndex];

                        Vector3 effectorMatrix = CalcBoneModelLocalPosition(ik.TargetBoneIndex);
                        Matrix4x4 modelLocalMat = CalcBoneModelLocalMatrix(attentionIndex);
                        if (!Matrix4x4.Invert(modelLocalMat, out Matrix4x4 inverseCoord))
                        {
                            continue;
                        }

                        float w = 1
                                  / (Vector3.Dot(effectorMatrix,
                                                 new Vector3(inverseCoord.M14, inverseCoord.M24, inverseCoord.M34))
                                     + inverseCoord.M44);

                        Vector3 localEffectorDir =
                            Vector3.Transform(effectorMatrix,
                                              inverseCoord) * w;

                        Vector3 localTargetDir =
                            Vector3.Transform(targetMatrix,
                                              inverseCoord) * w;

                        localEffectorDir = Vector3.Normalize(localEffectorDir);
                        localTargetDir = Vector3.Normalize(localTargetDir);
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

                        if (axis == Vector3.Zero)
                        {
                            continue;
                        }

                        axis = Vector3.Normalize(axis);

                        Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angle);
                        if (bone.Name == "左ひざ" || bone.Name == "右ひざ")
                        {
                            Quaternion rv = rotation * Quaternion.CreateFromRotationMatrix(bone.BoneMatBoneLocal);
                            Quaternion.Normalize(rv);
                            var eulerAngle = new EulerAngles(Matrix4x4.CreateFromQuaternion(rv));
                            eulerAngle.X = Clamp(eulerAngle.X, MathUtil.DegreeToRadian(-180.0f), MathUtil.DegreeToRadian(-10.0f));
                            eulerAngle.Y = 0;
                            eulerAngle.Z = 0;
                            bone.BoneMatBoneLocal = eulerAngle.CreateMatrix()
                                                    * Matrix4x4.CreateTranslation(bone.BoneMatBoneLocal.Translation);
                        }
                        else
                        {
                            bone.BoneMatBoneLocal = Matrix4x4.CreateFromQuaternion(rotation) * bone.BoneMatBoneLocal;
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

        private Matrix4x4 CalcBoneModelLocalMatrix(int index)
        {
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            Matrix4x4 res = bones[index].BoneMatBoneLocal;

            for (int parent = bones[index].ParentIndex; parent != -1; parent = bones[parent].ParentIndex)
            {
                res *= bones[parent].BoneMatBoneLocal;
            }

            return res;
        }

        private Vector3 CalcBoneModelLocalPosition(int index)
        {
            ObservableCollection<MmdModelModel.Bone> bones = _model.BoneKeyList;
            Vector3 res = bones[index].BoneMatBoneLocal.Translation;
            for (int parent = bones[index].ParentIndex; parent != -1; parent = bones[parent].ParentIndex)
            {
                Matrix4x4 mat = bones[parent].BoneMatBoneLocal;
                res = Vector3.Transform(res, mat);
            }

            return res;
        }

        #region WorldBoneプロパティ

        private Matrix4x4[] _worldBones;

        public Matrix4x4[] WorldBones => _worldBones;

        #endregion

        #region ModelLocalBonesプロパティ

        private Matrix4x4[] _modelLocalBones2;

        public Matrix4x4[] ModelLocalBones => _modelLocalBones2;

        #endregion

        public void InitBoneCalc()
        {
            InitBoneCalc(_model.BoneKeyList[0], Matrix4x4.Identity);
            foreach (var i in _model.BoneKeyList)
            {
                i.BoneMatBoneLocal = i.InitMatBoneLocal;
            }
        }

        private void InitBoneCalc(MmdModelModel.Bone me, Matrix4x4 parentoffsetMat)
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
