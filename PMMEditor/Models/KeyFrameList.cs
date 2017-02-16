using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Models
{
    public delegate void MoveChangedHandler(int index, int diff);

    public class KeyFrameBase : NotificationObject
    {
        public MoveChangedHandler MoveChanged;

        #region IsSelected変更通知プロパティ

        private bool _IsSelected;

        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                if (_IsSelected == value)
                {
                    return;
                }
                _IsSelected = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }

    public interface IKeyFrameList : IDictionary, INotifyCollectionChanged
    {
        bool CanSelectedFrameMove(int diff, bool isOverride = false);

        void SelectedFrameMove(int diff);
        
        string Name { get; }
    }

    public class KeyFrameList<T> : Dictionary<int, T>, IKeyFrameList where T : KeyFrameBase
    {
        public KeyFrameList(string name)
        {
            Name = name;
        }

        public string Name { get; }

        #region CanMoveメソッド

        private bool CanMove(int nowIndex, int diff, bool isOverride = false)
        {
            if (nowIndex + diff < 0)
            {
                return false;
            }
            if (!ContainsKey(nowIndex + diff))
            {
                return true;
            }
            return this[nowIndex + diff].IsSelected || isOverride;
        }

        private bool CanMoveAll(IEnumerable<int> nowIndex, int diff, bool isOverride = false)
        {
            return nowIndex.Any() == false || nowIndex.All(i => CanMove(i, diff, isOverride));
        }

        public bool CanSelectedFrameMove(int diff, bool isOverride = false)
        {
            var selectedIndex = ((Dictionary<int,T>) this).Where(v => v.Value.IsSelected).Select(v => v.Key);
            return CanMoveAll(selectedIndex, diff, isOverride);
        }

        #endregion

        #region Moveメソッド

        public void Move(KeyValuePair<int, T> nowIndex, int diff)
        {
            var p = nowIndex.Value;
            this[nowIndex.Key + diff] = p;
            p.MoveChanged?.Invoke(nowIndex.Key, diff);
            MoveChanged?.Invoke(nowIndex.Key, diff);
        }

        private void MoveAll(IEnumerable<KeyValuePair<int, T>> nowIndex, int diff)
        {
            foreach (var item in nowIndex)
            {
                Remove(item.Key);
            }
            foreach (var i in nowIndex)
            {
                Move(i, diff);
            }
        }

        public void SelectedFrameMove(int diff)
        {
            var selectedIndex = this.Where(v => v.Value.IsSelected).ToList();
            MoveAll(selectedIndex, diff);
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

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
