using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Livet;
using PMMEditor.MMDFileParser;
using PMMEditor.MVVM;

namespace PMMEditor.Models
{
    public abstract class KeyFrameBase : BindableBase
    {
        #region IsSelected変更通知プロパティ

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        #endregion

        #region FrameNumber変更通知プロパティ

        private int _frameNumber;

        public int FrameNumber
        {
            get { return _frameNumber; }
            set { SetProperty(ref _frameNumber, value); }
        }

        #endregion
    }

    public interface IKeyFrameInterpolationMethod<T>
    {
        T Interpolation(T left, T right, int frame);
    }

    public struct DefaultKeyFrameInterpolationMethod<T> : IKeyFrameInterpolationMethod<T>
    {
        public T Interpolation(T left, T right, int frame)
        {
            return left;
        }
    }

    public interface IKeyFrameList : IDictionary, INotifyCollectionChanged
    {
        bool CanSelectedFrameMove(int diff, bool isOverride = false);

        void SelectedFrameMove(int diff);

        string Name { get; }
    }

    public class KeyFrameList<T, InterpolationMethod> : NotificationObject, IDictionary<int, T>, IKeyFrameList
        where T : KeyFrameBase
        where InterpolationMethod : IKeyFrameInterpolationMethod<T>, new()
    {
        private readonly SortedDictionary<int, T> _item = new SortedDictionary<int, T>();
        private readonly InterpolationMethod _interpolationMethod = new InterpolationMethod();

        public KeyFrameList(string name)
        {
            Name = name;
        }

        public string Name { get; }

        #region MaxFrame変更通知プロパティ

        private int _maxFrame;

        public int MaxFrame
        {
            get { return _maxFrame; }
            set
            {
                if (_maxFrame == value)
                {
                    return;
                }

                _maxFrame = value;
                RaisePropertyChanged();
            }
        }

        #endregion

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

            return _item[nowIndex + diff].IsSelected || isOverride;
        }

        private bool CanMoveAll(IEnumerable<int> nowIndex, int diff, bool isOverride = false)
        {
            return nowIndex.Any() == false || nowIndex.All(i => CanMove(i, diff, isOverride));
        }

        public bool CanSelectedFrameMove(int diff, bool isOverride = false)
        {
            IEnumerable<int> selectedIndex = this.Where(v => v.Value.IsSelected).Select(v => v.Key);
            return CanMoveAll(selectedIndex, diff, isOverride);
        }

        #endregion

        #region Moveメソッド

        public void Move(KeyValuePair<int, T> nowIndex, int diff)
        {
            T p = nowIndex.Value;
            _item[nowIndex.Key + diff] = p;
            p.FrameNumber = nowIndex.Key + diff;
        }

        private void MoveAll(IEnumerable<KeyValuePair<int, T>> nowIndex, int diff)
        {
            foreach (var item in nowIndex)
            {
                _item.Remove(item.Key);
            }
            foreach (var i in nowIndex)
            {
                Move(i, diff);
            }
        }

        public void SelectedFrameMove(int diff)
        {
            List<KeyValuePair<int, T>> selectedIndex = this.Where(v => v.Value.IsSelected).ToList();
            MoveAll(selectedIndex, diff);
        }

        #endregion

        public T GetInterpolationData(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Negative number is out of range");
            }

            T preData = null;
            // TODO: 二分探索にする
            foreach (var item in this)
            {
                if (index < item.Key)
                {
                    if (preData.FrameNumber == index)
                    {
                        return preData;
                    }

                    T left = preData;
                    T right = item.Value;
                    return _interpolationMethod.Interpolation(left, right, index);
                }

                preData = item.Value;
            }

            return preData;
        }

        public void CreateKeyFrame<TIn>(
            TIn[] frame,
            TIn initFrame,
            Func<TIn, T> createFunc) where TIn : PmmStruct.IKeyFrame
        {
            Debug.Assert(initFrame != null && frame != null);

            Add(initFrame.FrameNumber, createFunc(initFrame));
            int next = initFrame.NextIndex;
            while (next != 0)
            {
                Add(frame[next].FrameNumber, createFunc(frame[next]));
                next = frame[next].NextIndex;
            }
        }


        public static TKeyFrame[] CreateKeyFrameArray<TKeyFrame>(List<TKeyFrame> boneKeyFrames)
            where TKeyFrame : PmmStruct.IKeyFrame
        {
            if (boneKeyFrames == null)
            {
                return null;
            }

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
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        /// <summary> コレクションを反復処理する列挙子を返します。 </summary>
        /// <returns> コレクションを反復処理するために使用できる <see cref = "T:System.Collections.IEnumerator" /> オブジェクト。 </returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary> コレクションを反復処理する列挙子を返します。 </summary>
        /// <returns> コレクションの反復処理に使用できる列挙子。 </returns>
        IEnumerator<KeyValuePair<int, T>> IEnumerable<KeyValuePair<int, T>>.GetEnumerator() => GetEnumerator();

        public IEnumerator<KeyValuePair<int, T>> GetEnumerator() => _item.GetEnumerator();

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に項目を追加します。
        /// </summary>
        /// <param name = "item">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に追加するオブジェクト。
        /// </param>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> は読み取り専用です。
        /// </exception>
        public void Add(KeyValuePair<int, T> item)
        {
            _item.Add(item.Key, item.Value);
            item.Value.FrameNumber = item.Key;
            MaxFrame = Math.Max(MaxFrame, item.Key);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> からすべての項目を削除します。
        /// </summary>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> は読み取り専用です。
        /// </exception>
        void ICollection<KeyValuePair<int, T>>.Clear() => Clear();

        public void Clear()
        {
            _item.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に特定の値が格納されているかどうかを判断します。
        /// </summary>
        /// <param name = "item">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> 内で検索するオブジェクト。
        /// </param>
        /// <returns>
        /// true が <paramref name = "item" /> に存在する場合は <see cref = "T:System.Collections.Generic.ICollection`1" />
        /// 。それ以外の場合は false。
        /// </returns>
        public bool Contains(KeyValuePair<int, T> item) => _item.Contains(item);

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> の要素を <see cref = "T:System.Array" /> にコピーします。
        /// <see cref = "T:System.Array" /> の特定のインデックスからコピーが開始されます。
        /// </summary>
        /// <param name = "array">
        /// <see cref = "T:System.Array" /> から要素がコピーされる 1 次元の <see cref = "T:System.Collections.Generic.ICollection`1" />。
        /// <see cref = "T:System.Array" /> には、0 から始まるインデックス番号が必要です。
        /// </param>
        /// <param name = "arrayIndex"> コピーの開始位置とする <paramref name = "array" /> のインデックス (0 から始まる)。 </param>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "array" /> は null です。
        /// </exception>
        /// <exception cref = "T:System.ArgumentOutOfRangeException">
        /// <paramref name = "arrayIndex" /> が 0 未満です。
        /// </exception>
        /// <exception cref = "T:System.ArgumentException">
        /// ソース内の要素の数 <see cref = "T:System.Collections.Generic.ICollection`1" />
        /// から使用可能な領域よりも大きい <paramref name = "arrayIndex" /> 変換先の末尾に <paramref name = "array" />します。
        /// </exception>
        /// <exception cref="NotImplementedException"></exception>
        public void CopyTo(KeyValuePair<int, T>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary> 特定のオブジェクトが <see cref = "T:System.Collections.Generic.ICollection`1" /> 内にあるときに、最初に出現したものを削除します。 </summary>
        /// <param name = "item">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> から削除するオブジェクト。
        /// </param>
        /// <returns>
        /// true が <paramref name = "item" /> から正常に削除された場合は <see cref = "T:System.Collections.Generic.ICollection`1" />
        /// 。それ以外の場合は false。 このメソッドは、false が元の <paramref name = "item" /> に見つからない場合にも
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> を返します。
        /// </returns>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> は読み取り専用です。
        /// </exception>
        public bool Remove(KeyValuePair<int, T> item)
        {
            return Remove(item.Key);
        }

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に格納されている要素の数を取得します。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に格納されている要素の数。
        /// </returns>
        int ICollection<KeyValuePair<int, T>>.Count => Count;

        public int Count => _item.Count;

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> が読み取り専用かどうかを示す値を取得します。
        /// </summary>
        /// <returns> true が読み取り専用である場合は <see cref = "T:System.Collections.Generic.ICollection`1" />。それ以外の場合は false。 </returns>
        bool ICollection<KeyValuePair<int, T>>.IsReadOnly => IsReadOnly;

        public bool IsReadOnly => false;

        /// <summary> 指定したキーの要素が <see cref = "T:System.Collections.Generic.IDictionary`2" /> に格納されているかどうかを確認します。 </summary>
        /// <param name = "key">
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" /> 内で検索されるキー。
        /// </param>
        /// <returns> 指定したキーを持つ要素を true が保持している場合は <see cref = "T:System.Collections.Generic.IDictionary`2" />。それ以外の場合は false。 </returns>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        public bool ContainsKey(int key) => _item.ContainsKey(key);

        /// <summary> 指定したキーおよび値を持つ要素を <see cref = "T:System.Collections.Generic.IDictionary`2" /> オブジェクトに追加します。 </summary>
        /// <param name = "key"> 追加する要素のキーとして使用するオブジェクト。 </param>
        /// <param name = "value"> 追加する要素の値として使用するオブジェクト。 </param>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        /// <exception cref = "T:System.ArgumentException">
        /// 同じキーを持つ要素が、<see cref = "T:System.Collections.Generic.IDictionary`2" />
        /// に既に存在します。
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" /> は読み取り専用です。
        /// </exception>
        public void Add(int key, T value)
        {
            Add(new KeyValuePair<int, T>(key, value));
        }

        /// <summary> 指定したキーを持つ要素を <see cref = "T:System.Collections.Generic.IDictionary`2" /> から削除します。 </summary>
        /// <param name = "key"> 削除する要素のキー。 </param>
        /// <returns>
        /// 要素が正常に削除された場合は true。それ以外の場合は false。  このメソッドは、元の false で <paramref name = "key" /> が見つからなかった場合にも
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" /> を返します。
        /// </returns>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" /> は読み取り専用です。
        /// </exception>
        public bool Remove(int key)
        {
            if (!_item.TryGetValue(key, out T val))
            {
                return false;
            }

            _item.Remove(key);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                                     new KeyValuePair<int, T>(key, val)));
            return true;
        }

        /// <summary> 指定したキーに関連付けられている値を取得します。 </summary>
        /// <param name = "key"> 値を取得する対象のキー。 </param>
        /// <param name = "value">
        /// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <paramref name = "value" />
        /// パラメーターの型に対する既定の値。 このパラメーターは初期化せずに渡されます。
        /// </param>
        /// <returns>
        /// true かどうか、オブジェクトを実装する <see cref = "T:System.Collections.Generic.IDictionary`2" /> 、指定した要素が含まれるキー以外の場合、
        /// falseです。
        /// </returns>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        public bool TryGetValue(int key, out T value)
        {
            return _item.TryGetValue(key, out value);
        }

        /// <summary> 指定したキーを持つ要素を取得または設定します。 </summary>
        /// <param name = "key"> 取得または設定する要素のキー。 </param>
        /// <returns> 指定したキーを持つ要素。 </returns>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        /// <exception cref = "T:System.Collections.Generic.KeyNotFoundException"> プロパティの取得と <paramref name = "key" /> が見つかりません。 </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// このプロパティが設定されていますが、
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" /> が読み取り専用です。
        /// </exception>
        T IDictionary<int, T>.this[int key]
        {
            get { return _item[key]; }
            set { _item[key] = value; }
        }

        public T this[int key]
        {
            get { return _item[key]; }
            set { _item[key] = value; }
        }

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> のキーを保持している
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" /> を取得します。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> を実装するオブジェクトのキーを含む
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" />します。
        /// </returns>
        ICollection<int> IDictionary<int, T>.Keys => Keys;

        public SortedDictionary<int, T>.KeyCollection Keys => _item.Keys;

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> 内の値を格納している
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" /> を取得します。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> を実装するオブジェクトの値を含む
        /// <see cref = "T:System.Collections.Generic.IDictionary`2" />します。
        /// </returns>
        ICollection<T> IDictionary<int, T>.Values => Values;

        public SortedDictionary<int, T>.ValueCollection Values => _item.Values;

        /// <summary>
        /// <see cref = "T:System.Collections.ICollection" /> の要素を <see cref = "T:System.Array" /> にコピーします。
        /// <see cref = "T:System.Array" /> の特定のインデックスからコピーが開始されます。
        /// </summary>
        /// <param name = "array">
        /// <see cref = "T:System.Collections.ICollection" /> から要素がコピーされる 1 次元の <see cref = "T:System.Array" />。
        /// <see cref = "T:System.Array" /> には、0 から始まるインデックス番号が必要です。
        /// </param>
        /// <param name = "index"> コピーの開始位置とする <paramref name = "array" /> のインデックス (0 から始まる)。 </param>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "array" /> は null です。
        /// </exception>
        /// <exception cref = "T:System.ArgumentOutOfRangeException">
        /// <paramref name = "index" /> が 0 未満です。
        /// </exception>
        /// <exception cref = "T:System.ArgumentException">
        /// <paramref name = "array" /> が多次元です。または ソース内の要素の数 <see cref = "T:System.Collections.ICollection" /> から使用可能な領域よりも大きい
        /// <paramref name = "index" /> 変換先の末尾に <paramref name = "array" />します。またはコピー元の
        /// <see cref = "T:System.Collections.ICollection" /> の型をコピー先の <paramref name = "array" /> の型に自動的にキャストすることはできません。
        /// </exception>
        /// <exception cref="NotImplementedException"></exception>
        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <see cref = "T:System.Collections.ICollection" /> に格納されている要素の数を取得します。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.ICollection" /> に格納されている要素の数。
        /// </returns>
        int ICollection.Count => Count;

        /// <summary>
        /// <see cref = "T:System.Collections.ICollection" /> へのアクセスを同期するために使用できるオブジェクトを取得します。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.ICollection" /> へのアクセスを同期するために使用できるオブジェクト。
        /// </returns>
        public object SyncRoot { get; } = null;

        /// <summary>
        /// <see cref = "T:System.Collections.ICollection" /> へのアクセスが同期されている (スレッド セーフである) かどうかを示す値を取得します。
        /// </summary>
        /// <returns> true へのアクセスが同期されている (スレッド セーフである) 場合は <see cref = "T:System.Collections.ICollection" />。それ以外の場合は false。 </returns>
        public bool IsSynchronized { get; } = false;

        /// <summary> 指定したキーを持つ要素が <see cref = "T:System.Collections.IDictionary" /> オブジェクトに格納されているかどうかを確認します。 </summary>
        /// <param name = "key">
        /// <see cref = "T:System.Collections.IDictionary" /> オブジェクト内で検索されるキー。
        /// </param>
        /// <returns> 指定したキーを持つ要素を true が保持している場合は <see cref = "T:System.Collections.IDictionary" />。それ以外の場合は false。 </returns>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        /// <exception cref="NotImplementedException"></exception>
        public bool Contains(object key)
        {
            throw new NotImplementedException();
        }

        /// <summary> 指定したキーおよび値を持つ要素を <see cref = "T:System.Collections.IDictionary" /> オブジェクトに追加します。 </summary>
        /// <param name = "key"> 追加する要素のキーとして使用する <see cref = "T:System.Object" />。 </param>
        /// <param name = "value"> 追加する要素の値として使用する <see cref = "T:System.Object" />。 </param>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        /// <exception cref = "T:System.ArgumentException">
        /// 同じキーを持つ要素が既に存在する、 <see cref = "T:System.Collections.IDictionary" />
        /// オブジェクトです。
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.IDictionary" /> は読み取り専用です。-または- <see cref = "T:System.Collections.IDictionary" />
        /// のサイズが固定されています。
        /// </exception>
        /// <exception cref="NotImplementedException"></exception>
        public void Add(object key, object value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <see cref = "T:System.Collections.IDictionary" /> オブジェクトからすべての要素を削除します。
        /// </summary>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.IDictionary" /> オブジェクトは読み取り専用です。
        /// </exception>
        void IDictionary.Clear()
        {
            Clear();
        }

        /// <summary>
        /// <see cref = "T:System.Collections.IDictionary" /> オブジェクトの <see cref = "T:System.Collections.IDictionaryEnumerator" />
        /// オブジェクトを返します。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.IDictionaryEnumerator" /> オブジェクトの <see cref = "T:System.Collections.IDictionary" />
        /// オブジェクト。
        /// </returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return _item.GetEnumerator();
        }

        /// <summary> 指定したキーを持つ要素を <see cref = "T:System.Collections.IDictionary" /> オブジェクトから削除します。 </summary>
        /// <param name = "key"> 削除する要素のキー。 </param>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.IDictionary" /> オブジェクトは読み取り専用です。または <see cref = "T:System.Collections.IDictionary" />
        /// のサイズが固定されています。
        /// </exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void Remove(object key)
        {
            throw new InvalidOperationException();
        }

        /// <summary> 指定したキーを持つ要素を取得または設定します。 </summary>
        /// <param name = "key"> 取得または設定する要素のキー。 </param>
        /// <returns> 指定したキーを持つ要素。該当するキーが存在しない場合は null。 </returns>
        /// <exception cref = "T:System.ArgumentNullException">
        /// <paramref name = "key" /> は null です。
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// プロパティが設定され、 <see cref = "T:System.Collections.IDictionary" />
        /// オブジェクトは読み取り専用です。または プロパティを設定すると、 <paramref name = "key" /> 、コレクションに存在しません、
        /// <see cref = "T:System.Collections.IDictionary" /> のサイズが固定されています。
        /// </exception>
        /// <exception cref="NotImplementedException"></exception>
        object IDictionary.this[object key]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// 取得、 <see cref = "T:System.Collections.ICollection" /> オブジェクトのキーを含む、
        /// <see cref = "T:System.Collections.IDictionary" /> オブジェクトです。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.ICollection" /> オブジェクトのキーを含む、 <see cref = "T:System.Collections.IDictionary" />
        /// オブジェクトです。
        /// </returns>
        ICollection IDictionary.Keys => Keys;

        /// <summary>
        /// 取得、 <see cref = "T:System.Collections.ICollection" /> オブジェクトの値を含む、
        /// <see cref = "T:System.Collections.IDictionary" /> オブジェクトです。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.ICollection" /> オブジェクトの値を含む、 <see cref = "T:System.Collections.IDictionary" />
        /// オブジェクトです。
        /// </returns>
        ICollection IDictionary.Values => Values;

        /// <summary> 示す値を取得するかどうか、 <see cref = "T:System.Collections.IDictionary" /> オブジェクトは読み取り専用です。 </summary>
        /// <returns> true 場合、 <see cref = "T:System.Collections.IDictionary" /> オブジェクトが読み取り専用でない場合は falseです。 </returns>
        bool IDictionary.IsReadOnly => IsReadOnly;

        /// <summary> 示す値を取得するかどうか、 <see cref = "T:System.Collections.IDictionary" /> オブジェクトのサイズが固定されています。 </summary>
        /// <returns> true 場合、 <see cref = "T:System.Collections.IDictionary" /> オブジェクトが固定サイズでない場合は falseです。 </returns>
        public bool IsFixedSize { get; } = false;
    }
}
