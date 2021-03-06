﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using PMMEditor.Models;
using Livet.Commands;
using PMMEditor.MVVM;
using PMMEditor.Views.Timeline;
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
            _keyFrameBase.PropertyChangedAsObservable().Subscribe(_ => RaisePropertyChanged(_.PropertyName))
                .AddTo(CompositeDisposables);
        }

        #region FrameNumber変更通知プロパティ

        public int FrameNumber => _keyFrameBase.FrameNumber;

        #endregion

        public bool IsSelected
        {
            get { return _keyFrameBase.IsSelected; }
            set { _keyFrameBase.IsSelected = value; }
        }
    }

    public class TimelineKeyFrameList : BindableDisposableBase
    {
        #region IsExpanded変更通知プロパティ

        private bool _isExpanded;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetProperty(ref _isExpanded, value); }
        }

        #endregion

        public ReadOnlyReactiveCollection<TimelineFrameData> Frame { get; set; }

        public string Name { get; set; }

        public ReadOnlyReactiveCollection<TimelineKeyFrameList> Children { get; set; }

        public static TimelineKeyFrameList Create<T, Method>(KeyFrameList<T, Method> list, string name)
            where T : KeyFrameBase
            where Method : IKeyFrameInterpolationMethod<T>, new()
        {
            var res = new TimelineKeyFrameList
            {
                Frame = list.ToReadOnlyReactiveCollection(
                    list.ToCollectionChanged<KeyValuePair<int, T>>(),
                    v => new TimelineFrameData(v.Value)),
                Name = name
            };
            res.CompositeDisposables.Add(res.Frame);
            return res;
        }

        public static TimelineKeyFrameList Create<T, Method>(KeyFrameList<T, Method> list)
            where T : KeyFrameBase
            where Method : IKeyFrameInterpolationMethod<T>, new()
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
                             .AddTo(CompositeDisposables);
        }

        #region ListOfKeyFrameList変更通知プロパティ

        public IList<TimelineKeyFrameList> ListOfKeyFrameList { get; set; }

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

        private GridNumberList _gridFrameNumberList = new GridNumberList();

        public GridNumberList GridFrameNumberList
        {
            get { return _gridFrameNumberList; }
            set { SetProperty(ref _gridFrameNumberList, value); }
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
