using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;

namespace PMMEditor.Models
{
    public class KeyFrameList<T> : Dictionary<int, T>
    {
        public KeyFrameList(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public bool CanMove(int nowIndex, int diff, bool isOverride = false)
        {
            if (nowIndex + diff < 0)
            {
                return false;
            }
            var p = this[nowIndex];
            if (p == null)
            {
                throw new NullReferenceException(nameof(p));
            }
            var next = this[nowIndex + diff];
            return next == null || isOverride;
        }

        public bool Move(int nowIndex, int diff, bool isOverride = false)
        {
            if (CanMove(nowIndex, diff, isOverride) == false)
            {
                return false;
            }
            var p = this[nowIndex];
            Remove(nowIndex);
            this[nowIndex + diff] = p;
            return true;
        }

        public async Task CreateKeyFrame<TIn>(TIn[] frame,
                                              TIn initFrame,
                                              Func<TIn, T> createFunc) where TIn : PmmStruct.IKeyFrame
        {
            if (initFrame == null)
            {
                return;
            }
            await Task.Run(() =>
            {
                Add(initFrame.FrameNumber, createFunc(initFrame));
                var next = initFrame.NextIndex;
                while (next != 0)
                {
                    Add(frame[next].FrameNumber, createFunc(frame[next]));
                    next = frame[next].NextIndex;
                }
            });
        }


        public static async Task<TKeyFrame[]> CreateKeyFrameArray<TKeyFrame>(List<TKeyFrame> boneKeyFrames)
            where TKeyFrame : PmmStruct.IKeyFrame
        {
            return await Task.Run(() =>
            {
                int maxDataIndex = 0;
                foreach (var item in boneKeyFrames)
                {
                    maxDataIndex = Math.Max(maxDataIndex, item.DataIndex);
                }
                var res = new TKeyFrame[maxDataIndex + 1];
                foreach (var item in boneKeyFrames)
                {
                    res[item.DataIndex] = item;
                }
                return res;
            });
        }
    }
}
