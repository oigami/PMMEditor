using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Documents;
using Livet;
using PMMEditor.Models;

namespace PMMEditor.ViewModels.Documents
{
    public abstract class TimelineViewModelBase : DocumentViewModelBase
    {
        protected readonly Model _model;

        protected static List<FrameData> CreateList<T>(List<T> frameList, T beginFrame) where T : PmmStruct.IKeyFrame
        {
            if (beginFrame == null)
            {
                return null;
            }
            var res = new List<FrameData>();
            var item = beginFrame;
            res.Add(new FrameData {FrameNumber = item.FrameNumber});
            int nowIndex = item.NextIndex;
            while (nowIndex != 0)
            {
                res.Add(new FrameData {FrameNumber = item.FrameNumber});
                item = frameList.FirstOrDefault(frame => frame.DataIndex == nowIndex);
                if (item == null)
                {
                    break;
                }
                nowIndex = item.NextIndex;
            }
            return res;
        }

        public TimelineViewModelBase(Model model)
        {
            _model = model;
        }

        public struct FrameData
        {
            public int FrameNumber { get; set; }
        }

        public struct KeyFrameList
        {
            public List<FrameData> Frame { get; set; }

            public string Name { get; set; }
        }

        #region ListOfKeyFrameList変更通知プロパティ

        private ObservableCollection<KeyFrameList> _ListOfKeyFrameList;

        public ObservableCollection<KeyFrameList> ListOfKeyFrameList
        {
            get { return _ListOfKeyFrameList; }
            set
            {
                if (_ListOfKeyFrameList == value)
                {
                    return;
                }
                _ListOfKeyFrameList = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
