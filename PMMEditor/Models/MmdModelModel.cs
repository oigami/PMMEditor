using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.MMDModel;
using PMMEditor.MVVM;
using PMMEditor.Views.Documents;
using SharpDX;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace PMMEditor.Models
{
    public class MmdModelModel : BindableBase
    {
        public MmdModelModel()
        {
            BoneCalculator = new MmdModelBoneCalculator(this);
        }

        private PmmStruct.ModelData _modelData;
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        public class BoneKeyFrame : KeyFrameBase
        {
            public Vector3 Position { get; set; }

            public Quaternion Quaternion { get; set; }

            public int[] InterpolationX { get; set; }

            public int[] InterpolationY { get; set; }

            public int[] InterpolationZ { get; set; }

            public int[] InterpolationRotation { get; set; }

            public bool IsPhysicsDisabled { get; set; }
        }

        public class Bone
        {
            public int sibling = -1;
            public int firstChild = -1;
            public int id;
            public int parent = -1;
            public PmdStruct.BoneKind type;
            public Matrix initMat;
            public Matrix boneMatML;
            public Matrix initMatML;
            public Matrix offsetMat;
            public Matrix boneMat;
            public string name;

            public KeyFrameList<BoneKeyFrame> KeyFrameList;
        }

        #region BonekeyListプロパティ

        private ObservableCollection<Bone> _BoneKeyList = new ObservableCollection<Bone>();


        public ObservableCollection<Bone> BoneKeyList
        {
            get { return _BoneKeyList; }
            set { SetProperty(ref _BoneKeyList, value); }
        }

        #endregion

        public string Name { get; private set; }

        public string NameEnglish { get; private set; }

        public string FilePath { get; private set; }

        public MmdModelBoneCalculator BoneCalculator { get; private set; }

        public async Task Set(PmmStruct.ModelData modelData)
        {
            _modelData = modelData;
            Name = modelData.Name;
            NameEnglish = modelData.NameEn;
            FilePath = modelData.Path;

            var data = Pmd.ReadFile(modelData.Path);
            await CreateBones(modelData, data.Bones);
            BoneCalculator.InitBoneCalc();
            BoneCalculator.Update();
        }

        private async Task CreateBones(PmmStruct.ModelData modelData, IList<PmdStruct.Bone> bones)
        {
            var keyFrame = await KeyFrameList<BoneKeyFrame>.CreateKeyFrameArray(modelData.BoneKeyFrames);
            _BoneKeyList.Clear();

            var res = await Task.WhenAll(bones.Select(
                async (item, id) =>
                    await Task.Run(() =>
                    {
                        var boneInitFrame =
                            modelData.BoneInitFrames.Zip(modelData.BoneName, (x, y) => (bone: x, name: y))
                                     .First(t => t.name == item.Name).bone;
                        return CreateBone(item, id, bones, keyFrame, boneInitFrame);
                    })));
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
            if (item.ParentBoneIndex != null)
            {
                ushort parentBoneIndex = (ushort) item.ParentBoneIndex;

                //自分と同じ親で自分よりあとのボーンが兄弟になる
                for (int j = i + 1; j < size; ++j)
                {
                    if (parentBoneIndex == inputBone[j].ParentBoneIndex)
                    {
                        outputBone.sibling = j;
                        break;
                    }
                }
                outputBone.parent = parentBoneIndex;
            }

            //自分が親になっていて一番早く現れるボーンが子になる
            foreach (int j in Enumerable.Range(0, size))
            {
                if (i == inputBone[j].ParentBoneIndex)
                {
                    outputBone.firstChild = j;
                    break;
                }
            }

            outputBone.name = item.Name;
            outputBone.id = i;
            outputBone.type = item.Kind;
            Matrix modelLocalInitMat = Matrix.Translation(item.Position.X, item.Position.Y, item.Position.Z);
            outputBone.initMatML = outputBone.boneMatML = outputBone.initMat = modelLocalInitMat; // モデルローカル座標系
            outputBone.offsetMat = Matrix.Invert(modelLocalInitMat);

            var list = new KeyFrameList<BoneKeyFrame>(item.Name);
            outputBone.KeyFrameList = list;
            Func<sbyte[], int[]> createArray4 = _ => new int[] {_[0], _[1], _[2], _[3]};
            list.CreateKeyFrame(keyFrame, boneInitFrame, listBone =>
            {
                var res = new BoneKeyFrame
                {
                    Position = new Vector3(listBone.Translation[0], listBone.Translation[1], listBone.Translation[2]),
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

            return outputBone;
        }
    }
}
