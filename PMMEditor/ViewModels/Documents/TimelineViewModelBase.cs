using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using Livet;
using PMMEditor.Models;
using Livet.Commands;
using PMMEditor.Views.Documents;

namespace PMMEditor.ViewModels.Documents
{
    public class TimelineFrameData : ViewModel
    {
        public TimelineFrameData(int frame = -1, bool isSelected = false)
        {
            FrameNumber = frame;
            IsSelected = isSelected;
        }

        #region FrameNumber変更通知プロパティ

        private int _FrameNumber;

        public int FrameNumber
        {
            get { return _FrameNumber; }
            set
            {
                if (_FrameNumber == value)
                {
                    return;
                }
                _FrameNumber = value;
                RaisePropertyChanged();
            }
        }

        #endregion

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

        protected List<TimelineFrameData> CreateList<T>(List<T> frameList, T beginFrame)
            where T : PmmStruct.IKeyFrame
        {
            var maxFrameWidth = MaxFrameIndex;
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
                item = frameList.FirstOrDefault(frame => frame.DataIndex == nowIndex);
                if (item == null)
                {
                    break;
                }
                maxFrameWidth = Math.Max(maxFrameWidth, item.FrameNumber);
                res.Add(new TimelineFrameData {FrameNumber = item.FrameNumber});
                nowIndex = item.NextIndex;
            }
            MaxFrameIndex = maxFrameWidth;
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

        #region MaxFrameIndex変更通知プロパティ

        private int _maxFrameIndex;

        public int MaxFrameIndex
        {
            get { return _maxFrameIndex; }
            set
            {
                if (_maxFrameIndex == value)
                {
                    return;
                }
                _maxFrameIndex = value;
                GridFrameNumberList.Resize(MaxFrameIndex);
                RaisePropertyChanged(nameof(GridFrameNumberList));
            }
        }

        #endregion

        public class GridNumberList : IEnumerable<int>
        {
            private int _size;

            public GridNumberList(int size = 0)
            {
                _size = size;
            }

            public void Resize(int size)
            {
                _size = size;
            }

            public IEnumerator<int> GetEnumerator()
            {
                int size = _size / 5;
                for (int i = 0; i <= size; i++)
                {
                    yield return i * 5;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #region GridFrameNumberList変更通知プロパティ

        private GridNumberList _GridFrameNumberList = new GridNumberList();

        public GridNumberList GridFrameNumberList
        {
            get { return _GridFrameNumberList; }
            set
            {
                if (_GridFrameNumberList == value)
                {
                    return;
                }
                _GridFrameNumberList = value;
                RaisePropertyChanged();
            }
        }

        #region KeyFrameMoveCommand

        public abstract ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveStartedCommand { get; protected set; }

        public abstract ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveDeltaCommand { get; protected set; }

        public abstract ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveCompletedCommand { get; protected set; }

        #endregion

        #endregion
    }
}
