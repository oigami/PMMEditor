using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using PMMEditor.ECS;
using PMMEditor.Log;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.Graphics;
using PMMEditor.MVVM;
using SharpDX;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace PMMEditor.Models.MMDModel
{
    public class MmdModelModel : Component
    {
        private ILogger _logger;

        private bool _isInitialized;

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { SetProperty(ref _isInitialized, value); }
        }

        public void Initialize(ILogger logger)
        {
            _logger = logger;
        }

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

            public PmxStruct.Bone.Flag Type { get; set; }

            public Vector3 InitMatBoneLocal { get; set; }

            public Vector3 InitMatModelLocal { get; set; }

            public Vector3 InverseInitMatModelLocal { get; set; }

            public Matrix4x4 BoneMatBoneLocal { get; set; }

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

        public List<(PmxStruct.Bone, int)> IKList { get; private set; }

        public List<PmxStruct.Material> Materials { get; set; }

        public List<PmxStruct.Vertex> Vertices { get; set; }

        public List<int> Indices { get; set; }

        public string FilePath { get; set; }

        public IList<string> TextureFilePath { get; set; }

        public void Set(FileBlob blob, PmmStruct.ModelData modelData = null)
        {
            try
            {
                string filePath = blob.Path;
                byte[] fileData = blob.Data;
                MmdFileKind kind = blob.Kind;
                PmxStruct data;
                switch (kind)
                {
                    case MmdFileKind.Pmd:
                        data = Pmd.Read(fileData).ToPmx();
                        break;
                    case MmdFileKind.Pmx:
                        data = Pmx.Read(fileData);
                        break;
                    case MmdFileKind.Unknown:
                    case MmdFileKind.Pmm:
                    default:
                        throw new ArgumentException(filePath);
                }

                FilePath = filePath;
                Indices = data.Indices;
                Vertices = data.Vertices;
                Materials = data.Materials;
                TextureFilePath = data.TexturePath;
                Name = data.Name;
                NameEnglish = data.EnglishName;
                IKList = data.Bones.Indexed().Where(_ => (_.Item1.Flags & PmxStruct.Bone.Flag.IkFlag) != 0).ToList();
                CreateBones(modelData, data.Bones);
                IsInitialized = true;
            }
            catch (Exception e)
            {
                _logger.Error("Model Load Error", e);
            }
        }

        public Task SetAsync(PmmStruct.ModelData modelData)
        {
            return Task.Run(() => Set(new FileBlob(modelData.Path), modelData));
        }

        private void CreateBones(PmmStruct.ModelData modelData, IList<PmxStruct.Bone> bones)
        {
            PmmStruct.ModelData.BoneInitFrame[] keyFrame =
                KeyFrameList<BoneKeyFrame, DefaultKeyFrameInterpolationMethod<BoneKeyFrame>>
                    .CreateKeyFrameArray(modelData?.BoneKeyFrames);
            BoneKeyList.Clear();

            IEnumerable<Task<Bone>> res = bones.Select((item, id) => Task.Run(() =>
            {
                PmmStruct.ModelData.BoneInitFrame boneInitFrame =
                    modelData?.BoneInitFrames
                              .Zip(modelData.BoneName, (x, y) => (bone: x, name: y))
                              .First(t => t.name == item.Name).bone;
                return CreateBone(item, id, bones, keyFrame, boneInitFrame);
            }));
            foreach (var item in res)
            {
                BoneKeyList.Add(item.Result);
            }
        }

        private Bone CreateBone(
            PmxStruct.Bone item, int i, IList<PmxStruct.Bone> inputBone,
            PmmStruct.ModelData.BoneInitFrame[] keyFrame,
            PmmStruct.ModelData.BoneInitFrame boneInitFrame)
        {
            var outputBone = new Bone();
            int size = inputBone.Count;
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
            outputBone.Type = item.Flags;
            outputBone.TailChildIndex = (item.Flags & PmxStruct.Bone.Flag.Connection) != 0
                ? item.ConnectionBoneIndex
                : -1;
            outputBone.InitMatModelLocal = item.Position;
            outputBone.InverseInitMatModelLocal = Vector3.Negate(item.Position);

            var list = new KeyFrameList<BoneKeyFrame, KeyInterpolationMethod>(item.Name);
            outputBone.KeyFrameList = list;

            if (boneInitFrame == null)
            {
                list.Add(0, new BoneKeyFrame
                {
                    FrameNumber = 0,
                    Position = Vector3.Zero,
                    Quaternion = Quaternion.Identity,
                    IsPhysicsDisabled = false,
                    IsSelected = false
                });
            }
            else
            {
                int[] CreateArray4(sbyte[] _) => new int[] { _[0], _[1], _[2], _[3] };

                list.CreateKeyFrame(keyFrame, boneInitFrame, listBone => new BoneKeyFrame
                {
                    Position =
                        new Vector3(listBone.Translation[0], listBone.Translation[1], listBone.Translation[2]),
                    Quaternion =
                        new Quaternion(listBone.Quaternion[0], listBone.Quaternion[1], listBone.Quaternion[2],
                                       listBone.Quaternion[3]),
                    InterpolationX = CreateArray4(listBone.InterpolationX),
                    InterpolationY = CreateArray4(listBone.InterpolationY),
                    InterpolationZ = CreateArray4(listBone.InterpolationZ),
                    InterpolationRotation = CreateArray4(listBone.InterpolationRotation),
                    IsPhysicsDisabled = listBone.IsPhysicsDisabled,
                    IsSelected = listBone.IsSelected
                });
            }

            return outputBone;
        }
    }
}
