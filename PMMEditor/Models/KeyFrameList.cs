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
        public bool Move(int nowIndex, int diff, bool isOverride = false)
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
            Remove(nowIndex);
            var next = this[nowIndex + diff];
            if (next != null && isOverride == false)
            {
                return false;
            }
            this[nowIndex + diff] = p;
            return true;
        }

        public async Task CreateKeyFrame<TIn>(TIn[] frame,
                                              TIn initFrame,
                                              Func<TIn, T>
                                                  createFunc) where TIn : PmmStruct.IKeyFrame
        {
            if (initFrame == null)
            {
                return;
            }
            await Task.Run(() =>
            {
                var next = initFrame.NextIndex;
                while (next != 0)
                {
                    Add(frame[next].FrameNumber, createFunc(initFrame));
                    next = frame[next].NextIndex;
                }
            });
        }
    }
}
