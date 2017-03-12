using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using PMMEditor.Models;
using Livet.Commands;
using PMMEditor.MVVM;
using PMMEditor.Views.Documents;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Documents
{
    public class TimelineFrameData : BindableDisposableBase
    {
        private readonly KeyFrameBase _keyFrameBase;

        public TimelineFrameData(KeyFrameBase keyFrameBase)
        {
            _keyFrameBase = keyFrameBase;

            _keyFrameBase.ObserveProperty(x => x.FrameNumber)
                         .Subscribe(x => RaisePropertyChanged(nameof(FrameNumber)))
                         .AddTo(CompositeDisposable);

            _keyFrameBase.ObserveProperty(x => x.IsSelected)
                         .Subscribe(x => RaisePropertyChanged(nameof(IsSelected)))
                         .AddTo(CompositeDisposable);
        }

        #region FrameNumber変更通知プロパティ

        public int FrameNumber => _keyFrameBase.FrameNumber;

        #endregion

        public bool IsSelected
        {
            get { return _keyFrameBase.IsSelected; }
            set
            {
                if (_keyFrameBase.IsSelected == value)
                {
                    return;
                }
                _keyFrameBase.IsSelected = value;
                RaisePropertyChanged();
            }
        }
    }

    public class TimelineKeyFrameList : BindableDisposableBase
    {
        #region IsExpanded変更通知プロパティ

        private bool _IsExpanded;

        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set { SetProperty(ref _IsExpanded, value); }
        }

        #endregion

        public ReadOnlyReactiveCollection<TimelineFrameData> Frame { get; set; }

        public string Name { get; set; }

        public ReadOnlyReactiveCollection<TimelineKeyFrameList> Children { get; set; }

        public static TimelineKeyFrameList Create<T>(KeyFrameList<T> list, string name) where T : KeyFrameBase
        {
            var res = new TimelineKeyFrameList
            {
                Frame = list.ToReadOnlyReactiveCollection(
                    list.ToCollectionChanged<KeyValuePair<int, T>>(),
                    v => new TimelineFrameData(v.Value)),
                Name = name
            };
            res.CompositeDisposable.Add(res.Frame);
            return res;
        }

        public static TimelineKeyFrameList Create<T>(KeyFrameList<T> list) where T : KeyFrameBase
        {
            return Create(list, list.Name);
        }
    }

    public abstract class TimelineViewModelBase : DocumentViewModelBase
    {
        protected readonly Model _model;

        public TimelineViewModelBase(Model model)
        {
            _model = model;
            NowFrame = _model.FrameControlModel.ObserveProperty(_ => _.NowFrame).ToReadOnlyReactiveProperty()
                             .AddTo(CompositeDisposable);
        }

        #region ListOfKeyFrameList変更通知プロパティ

        public ReadOnlyMultiCollection<TimelineKeyFrameList> ListOfKeyFrameList { get; set; }

        #endregion

        #region MaxFrameIndex変更通知プロパティ

        public ReactiveProperty<int> MaxFrameIndex { get; set; } = new ReactiveProperty<int>();

        #endregion

        public class GridNumberList : IEnumerable<int>, INotifyCollectionChanged
        {
            private int _size;

            public GridNumberList(int size = 0)
            {
                _size = size;
            }

            public void Resize(int size)
            {
                _size = size;
                CollectionChanged?.Invoke(this,
                                          new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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

            public event NotifyCollectionChangedEventHandler CollectionChanged;
        }

        #region GridFrameNumberList変更通知プロパティ

        private GridNumberList _GridFrameNumberList = new GridNumberList();

        public GridNumberList GridFrameNumberList
        {
            get { return _GridFrameNumberList; }
            set { SetProperty(ref _GridFrameNumberList, value); }
        }

        #endregion

        #region KeyFrameMoveCommand

        public abstract ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveStartedCommand { get; protected set; }

        public abstract ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveDeltaCommand { get; protected set; }

        public abstract ListenerCommand<KeyFrameMoveEventArgs> KeyFrameMoveCompletedCommand { get; protected set; }

        #endregion

        public ReadOnlyReactiveProperty<int> NowFrame { get; }
    }
}
