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
    public struct TimelineFrameData
    {
        public int FrameNumber { get; set; }
    }

    public class TimelineKeyFrameList : ViewModel
    {
        #region IsExpanded変更通知プロパティ

        private bool _IsExpanded;

        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set
            {
                if (_IsExpanded == value)
                {
                    return;
                }
                _IsExpanded = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        public List<TimelineFrameData> Frame { get; set; }

        public string Name { get; set; }

        public List<TimelineKeyFrameList> Children { get; set; }
    }

    public abstract class TimelineViewModelBase : DocumentViewModelBase
    {
        protected readonly Model _model;

        protected static List<TimelineFrameData> CreateList<T>(List<T> frameList, T beginFrame)
            where T : PmmStruct.IKeyFrame
        {
            if (beginFrame == null)
            {
                return null;
            }
            var res = new List<TimelineFrameData>();
            var item = beginFrame;
            res.Add(new TimelineFrameData {FrameNumber = item.FrameNumber});
            int nowIndex = item.NextIndex;
            while (nowIndex != 0)
            {
                res.Add(new TimelineFrameData {FrameNumber = item.FrameNumber});
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

        #region ListOfKeyFrameList変更通知プロパティ

        private ObservableCollection<TimelineKeyFrameList> _ListOfKeyFrameList;

        public ObservableCollection<TimelineKeyFrameList> ListOfKeyFrameList
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
