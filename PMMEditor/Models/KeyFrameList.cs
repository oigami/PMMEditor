using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;

namespace PMMEditor.Models
{
    public delegate void MoveChangedHandler(int index, int diff);

    public class KeyFrameBase : NotificationObject
    {
        public MoveChangedHandler MoveChanged;
    }

    public class KeyFrameList<T> : Dictionary<int, T> where T : KeyFrameBase
    {
        private readonly HashSet<int> _selectedItems = new HashSet<int>();

        public void Select(int index, bool isSelect)
        {
            if (isSelect)
            {
                _selectedItems.Add(index);
            }
            else
            {
                _selectedItems.Remove(index);
            }
        }

        public KeyFrameList(string name)
        {
            Name = name;
        }

        public string Name { get; }

        #region CanMoveメソッド

        public bool CanMove(int nowIndex, int diff, bool isOverride = false)
        {
            if (nowIndex + diff < 0)
            {
                return false;
            }
            if (ContainsKey(nowIndex) == false)
            {
                return false;
            }
            return !ContainsKey(nowIndex + diff) || isOverride;
        }

        public bool CanMoveAll(IEnumerable<int> nowIndex, int diff, bool isOverride = false)
        {
            return nowIndex.Any() == false || nowIndex.All(i => CanMove(i, diff, isOverride));
        }

        public bool CanSelectedFrameMove(int diff, bool isOverride = false)
        {
            return CanMoveAll(_selectedItems, diff, isOverride);
        }

        #endregion

        #region Moveメソッド

        public bool Move(int nowIndex, int diff, bool isOverride = false)
        {
            if (CanMove(nowIndex, diff, isOverride) == false)
            {
                return false;
            }
            var p = this[nowIndex];
            Remove(nowIndex);
            this[nowIndex + diff] = p;
            p.MoveChanged(nowIndex, diff);
            MoveChanged?.Invoke(nowIndex, diff);
            return true;
        }

        public void MoveAll(IEnumerable<int> nowIndex, int diff, bool isOverride = false)
        {
            foreach (var i in nowIndex)
            {
                Move(i, diff, isOverride);
            }
        }

        public void SelectedFrameMove(int diff, bool isOverride = false)
        {
            MoveAll(_selectedItems, diff, isOverride);
        }

        #endregion

        public event MoveChangedHandler MoveChanged;

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
