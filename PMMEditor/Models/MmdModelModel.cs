﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Livet;

namespace PMMEditor.Models
{
    public class MmdModelModel : NotificationObject
    {
        private PmmStruct.ModelData _modelData;
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        public class BoneKeyFrame
        {
            public Point3D Position { get; set; }

            public Quaternion Quaternion { get; set; }

            public int[] InterpolationX { get; set; }

            public int[] InterpolationY { get; set; }

            public int[] InterpolationZ { get; set; }

            public int[] InterpolationRotation { get; set; }

            public bool IsPhysicsDisabled { get; set; }

            public bool IsSelected { get; set; }
        }

        private readonly List<KeyFrameList<BoneKeyFrame>> _boneKeyList = new List<KeyFrameList<BoneKeyFrame>>();

        #region BonekeyListプロパティ

        private ReadOnlyCollection<KeyFrameList<BoneKeyFrame>> _BoneKeyList;

        public ReadOnlyCollection<KeyFrameList<BoneKeyFrame>> BoneKeyList
        {
            get { return _BoneKeyList; }
            set
            {
                if (_BoneKeyList == value)
                {
                    return;
                }
                _BoneKeyList = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        public async Task Set(PmmStruct.ModelData modelData)
        {
            _modelData = modelData;
            var keyFrame = await Task.Run(() =>
            {
                int maxDataIndex = 0;
                foreach (var item in modelData.BoneKeyFrames)
                {
                    maxDataIndex = Math.Max(maxDataIndex, item.DataIndex);
                }
                var res = new PmmStruct.ModelData.BoneInitFrame[maxDataIndex];
                foreach (var item in modelData.BoneKeyFrames)
                {
                    res[item.DataIndex] = item;
                }
                return res;
            });
            _boneKeyList.Clear();
            _boneKeyList.AddRange(await Task.WhenAll(modelData.BoneInitFrames.Zip(modelData.BoneName, async (x, y) =>
            {
                var list = new KeyFrameList<BoneKeyFrame>(y);
                Func<sbyte[], int[]> createArray4 = i => new int[] {i[0], i[1], i[2], i[3]};

                await list.CreateKeyFrame(keyFrame, x, i =>
                {
                    var res = new BoneKeyFrame
                    {
                        Position = new Point3D(i.Translation[0], i.Translation[1], i.Translation[2]),
                        Quaternion = new Quaternion(i.Quaternion[0], i.Quaternion[1], i.Quaternion[2], i.Quaternion[3]),
                        InterpolationX = createArray4(i.InterpolationX),
                        InterpolationY = createArray4(i.InterpolationY),
                        InterpolationZ = createArray4(i.InterpolationZ),
                        InterpolationRotation = createArray4(i.InterpolationRotation),
                        IsPhysicsDisabled = i.IsPhysicsDisabled,
                        IsSelected = i.IsSelected
                    };
                    return res;
                });
                return list;
            })));
            BoneKeyList = new ReadOnlyCollection<KeyFrameList<BoneKeyFrame>>(_boneKeyList);
        }
    }
}