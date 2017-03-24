﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Livet;
using PMMEditor.Log;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.MMDModel;
using PMMEditor.MVVM;
using SharpDX;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace PMMEditor.Models
{
    public class MmdModelModel : BindableBase
    {
        private readonly ILogger _logger;

        private bool _isInitialized;

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { SetProperty(ref _isInitialized, value); }
        }

        public MmdModelModel(ILogger logger)
        {
            _logger = logger;
        }

        private PmmStruct.ModelData _modelData;

        #region ボーン構造体

        public class BoneKeyFrame : KeyFrameBase
        {
            public Vector3 Position { get; set; }

            public Quaternion Quaternion { get; set; }

            public int[] InterpolationX { get; set; }

            public int[] InterpolationY { get; set; }

            public int[] InterpolationZ { get; set; }

            public int[] InterpolationRotation { get; set; }

            public bool IsPhysicsDisabled { get; set; }

            public void CopyTo(BoneKeyFrame notNullFrame)
            {
                notNullFrame.Position = Position;
                notNullFrame.Quaternion = Quaternion;
            }

        }

        public class KeyInterpolationMethod : IKeyFrameInterpolationMethod<BoneKeyFrame>
        {
            private readonly BoneKeyFrame _res = new BoneKeyFrame();

            public BoneKeyFrame Interpolation(BoneKeyFrame left, BoneKeyFrame right, int frame)
            {
                int diff = right.FrameNumber - left.FrameNumber;
                // TODO: 補完曲線に対応する（ニュートン法）
                float t = (float) (frame - left.FrameNumber) / diff;
                _res.Position = (right.Position - left.Position) * new Vector3(t, t, t) + left.Position;
                _res.Quaternion = Quaternion.Slerp(left.Quaternion, right.Quaternion, t);
                return _res;
            }
        }

        public class Bone
        {
            public int SiblingIndex { get; set; } = -1;

            public int FirstChildIndex { get; set; } = -1;

            public int Index { get; set; }

            public int ParentIndex { get; set; } = -1;

            public int TailChildIndex { get; set; }

            public PmdStruct.BoneKind Type { get; set; }

            public Matrix InitMatBoneLocal { get; set; }

            public Matrix BoneMatModelLocal { get; set; }

            public Matrix InitMatModelLocal { get; set; }

            public Matrix InverseInitMatModelLocal { get; set; }

            public Matrix BoneMatBoneLocal { get; set; }

            public string Name { get; set; }

            public KeyFrameList<BoneKeyFrame, KeyInterpolationMethod> KeyFrameList;
        }

        #endregion

        #region BonekeyListプロパティ

        public ObservableCollection<Bone> BoneKeyList { get; } = new ObservableCollection<Bone>();

        #endregion

        #region Name変更通知プロパティ

        private string _name;

        public string Name
        {
            get { return _name; }
            private set { SetProperty(ref _name, value); }
        }

        #endregion

        #region NameEnglish変更通知プロパティ

        private string _nameEnglish;

        public string NameEnglish
        {
            get { return _nameEnglish; }
            private set { SetProperty(ref _nameEnglish, value); }
        }

        #endregion

        #region FilePath変更通知プロパティ

        private string _filePath;

        public string FilePath
        {
            get { return _filePath; }
            private set { SetProperty(ref _filePath, value); }
        }

        #endregion

        public List<PmdStruct.IK> IKList { get; private set; }

        public Task SetAsync(string filePath)
        {
            return Task.Run(() => Set(filePath));
        }

        public void Set(string filePath)
        {
            try
            {
                var pmd = Pmd.ReadFile(filePath);
                Name = pmd.ModelName;
                NameEnglish = pmd.EnglishName?.ModelName;
                IKList = pmd.IKs;
                FilePath = filePath;
                CreateBones(null, pmd.Bones);
                IsInitialized = true;
            }
            catch (Exception e)
            {
                _logger.Error("Model Load Error", e);
            }
        }

        public async Task Set(PmmStruct.ModelData modelData)
        {
            _modelData = modelData;
            Name = modelData.Name;
            NameEnglish = modelData.NameEn;
            FilePath = modelData.Path;

            var data = Pmd.ReadFile(modelData.Path);
            IKList = data.IKs;
            await CreateBonesAsync(modelData, data.Bones);
        }

        private Task CreateBonesAsync(PmmStruct.ModelData modelData, IList<PmdStruct.Bone> bones)
        {
            return Task.Run(() => CreateBones(modelData, bones));
        }

        private void CreateBones(PmmStruct.ModelData modelData, IList<PmdStruct.Bone> bones)
        {
            var keyFrame =
                KeyFrameList<BoneKeyFrame, DefaultKeyFrameInterpolationMethod<BoneKeyFrame>>
                    .CreateKeyFrameArray(modelData?.BoneKeyFrames).Result;
            BoneKeyList.Clear();

            var res = Task.WhenAll(bones.Select(
                async (item, id) =>
                    await Task.Run(() =>
                    {
                        var boneInitFrame =
                            modelData?.BoneInitFrames.Zip(modelData?.BoneName, (x, y) => (bone: x, name: y))
                                      .First(t => t.name == item.Name).bone;
                        return CreateBone(item, id, bones, keyFrame, boneInitFrame);
                    }))).Result;
            foreach (var item in res)
            {
                BoneKeyList.Add(item);
            }
        }

        private Bone CreateBone(
            PmdStruct.Bone item, int i, IList<PmdStruct.Bone> inputBone,
            PmmStruct.ModelData.BoneInitFrame[] keyFrame,
            PmmStruct.ModelData.BoneInitFrame boneInitFrame)
        {
            var outputBone = new Bone();
            var size = inputBone.Count;
            //自分と同じ親で自分よりあとのボーンが兄弟になる
            for (int j = i + 1; j < size; ++j)
            {
                if (item.ParentBoneIndex == inputBone[j].ParentBoneIndex)
                {
                    outputBone.SiblingIndex = j;
                    break;
                }
            }
            outputBone.ParentIndex = item.ParentBoneIndex ?? -1;

            //自分が親になっていて一番早く現れるボーンが子になる
            foreach (int j in Enumerable.Range(0, size))
            {
                if (i == inputBone[j].ParentBoneIndex)
                {
                    outputBone.FirstChildIndex = j;
                    break;
                }
            }

            outputBone.Name = item.Name;
            outputBone.Index = i;
            outputBone.Type = item.Kind;
            outputBone.TailChildIndex = item.TailBoneIndex ?? -1;
            var modelLocalInitMat = Matrix.Translation(item.Position.X, item.Position.Y, item.Position.Z);
            outputBone.InitMatModelLocal =
                outputBone.BoneMatModelLocal = outputBone.InitMatBoneLocal = modelLocalInitMat; // モデルローカル座標系
            outputBone.InverseInitMatModelLocal = Matrix.Invert(modelLocalInitMat);

            var list = new KeyFrameList<BoneKeyFrame, KeyInterpolationMethod>(item.Name);
            outputBone.KeyFrameList = list;
            Func<sbyte[], int[]> createArray4 = _ => new int[] { _[0], _[1], _[2], _[3] };
            if (boneInitFrame == null)
            {
                list.Add(0, new BoneKeyFrame
                {
                    FrameNumber = 0,
                    Position = new Vector3(0, 0, 0),
                    Quaternion = new Quaternion(),
                    IsPhysicsDisabled = false,
                    IsSelected = false
                });
            }
            else
            {
                list.CreateKeyFrame(keyFrame, boneInitFrame, listBone =>
                {
                    var res = new BoneKeyFrame
                    {
                        Position =
                            new Vector3(listBone.Translation[0], listBone.Translation[1], listBone.Translation[2]),
                        Quaternion =
                            new Quaternion(listBone.Quaternion[0], listBone.Quaternion[1], listBone.Quaternion[2],
                                           listBone.Quaternion[3]),
                        InterpolationX = createArray4(listBone.InterpolationX),
                        InterpolationY = createArray4(listBone.InterpolationY),
                        InterpolationZ = createArray4(listBone.InterpolationZ),
                        InterpolationRotation = createArray4(listBone.InterpolationRotation),
                        IsPhysicsDisabled = listBone.IsPhysicsDisabled,
                        IsSelected = listBone.IsSelected
                    };
                    return res;
                });
            }

            return outputBone;
        }
    }
}
