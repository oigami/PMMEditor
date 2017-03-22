using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using Livet;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PMMEditor.ViewModels.Documents
{
    public sealed class ReadOnlyMultiCollection<T> : IList<T>, INotifyCollectionChanged,
                                                     IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        private readonly IList<T> _self;
        private readonly IList<T> _other;
        readonly List<int> _countList = new List<int>(2);

        private void CollectionChange(CollectionChanged<T> i)
        {
            if (i.Action == NotifyCollectionChangedAction.Reset)
            {
                DispatcherHelper.UIDispatcher.Invoke(
                    () =>
                        CollectionChanged?.Invoke(this,
                                                  new NotifyCollectionChangedEventArgs(
                                                      NotifyCollectionChangedAction.Reset)));
            }
            else
            {
                NotifyCollectionChangedEventArgs args;
                if (i.Action != NotifyCollectionChangedAction.Move)
                {
                    args = new NotifyCollectionChangedEventArgs(i.Action, i.Value, i.Index);
                }
                else
                {
                    args = new NotifyCollectionChangedEventArgs(i.Action, i.Value, i.Index,
                                                                i.OldIndex);
                }
                DispatcherHelper.UIDispatcher.Invoke(() => CollectionChanged?.Invoke(this, args));
            }
        }

        public ReadOnlyMultiCollection(IList<T> self, IList<T> other)
        {
            _self = self;
            _other = other;
            _countList.Add(_self.Count);
            _countList.Add(_other.Count);

            ((INotifyCollectionChanged) _self).ToCollectionChanged<T>().Subscribe(i =>
            {
                _countList[0] = _self.Count;
                CollectionChange(i);
            }).AddTo(_disposable);

            ((INotifyCollectionChanged) _other).ToCollectionChanged<T>().Subscribe(i =>
            {
                _countList[1] = _other.Count;

                i.Index += _countList[0];
                i.OldIndex += _countList[0];
                CollectionChange(i);
            }).AddTo(_disposable);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary> アンマネージ リソースの解放またはリセットに関連付けられているアプリケーション定義のタスクを実行します。 </summary>
        public void Dispose()
        {
            _disposable.Dispose();
        }

        /// <summary> コレクションを反復処理する列挙子を返します。 </summary>
        /// <returns> コレクションを反復処理するために使用できる <see cref = "T:System.Collections.IEnumerator" /> オブジェクト。 </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary> コレクションを反復処理する列挙子を返します。 </summary>
        /// <returns> コレクションの反復処理に使用できる列挙子。 </returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _self)
            {
                yield return item;
            }
            foreach (var item in _other)
            {
                yield return item;
            }
        }

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に項目を追加します。
        /// </summary>
        /// <param name = "item">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に追加するオブジェクト。
        /// </param>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> は読み取り専用です。
        /// </exception>
        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> からすべての項目を削除します。
        /// </summary>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> は読み取り専用です。
        /// </exception>
        public void Clear()
        {
            throw new NotImplementedException();
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
        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

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
        public void CopyTo(T[] array, int arrayIndex)
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
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に格納されている要素の数を取得します。
        /// </summary>
        /// <returns>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> に格納されている要素の数。
        /// </returns>
        public int Count => _self.Count + _other.Count;

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.ICollection`1" /> が読み取り専用かどうかを示す値を取得します。
        /// </summary>
        /// <returns> true が読み取り専用である場合は <see cref = "T:System.Collections.Generic.ICollection`1" />。それ以外の場合は false。 </returns>
        public bool IsReadOnly { get; } = true;

        /// <summary>
        /// <see cref = "T:System.Collections.Generic.IList`1" /> 内の特定の項目のインデックスを確認します。
        /// </summary>
        /// <param name = "item">
        /// <see cref = "T:System.Collections.Generic.IList`1" /> 内で検索するオブジェクト。
        /// </param>
        /// <returns> リストに存在する場合は <paramref name = "item" /> のインデックス。それ以外の場合は -1。 </returns>
        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        /// <summary> 指定したインデックスの <see cref = "T:System.Collections.Generic.IList`1" /> に項目を挿入します。 </summary>
        /// <param name = "index">
        /// <paramref name = "item" /> を挿入する位置の、0 から始まるインデックス。
        /// </param>
        /// <param name = "item">
        /// <see cref = "T:System.Collections.Generic.IList`1" /> に挿入するオブジェクト。
        /// </param>
        /// <exception cref = "T:System.ArgumentOutOfRangeException">
        /// <paramref name = "index" /> が <see cref = "T:System.Collections.Generic.IList`1" /> の有効なインデックスではありません。
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.IList`1" /> は読み取り専用です。
        /// </exception>
        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        /// <summary> 指定したインデックスにある <see cref = "T:System.Collections.Generic.IList`1" /> 項目を削除します。 </summary>
        /// <param name = "index"> 削除する項目の 0 から始まるインデックス。 </param>
        /// <exception cref = "T:System.ArgumentOutOfRangeException">
        /// <paramref name = "index" /> が <see cref = "T:System.Collections.Generic.IList`1" /> の有効なインデックスではありません。
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// <see cref = "T:System.Collections.Generic.IList`1" /> は読み取り専用です。
        /// </exception>
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        /// <summary> 指定したインデックスにある要素を取得または設定します。 </summary>
        /// <param name = "index"> 取得または設定する要素の、0 から始まるインデックス番号。 </param>
        /// <returns> 指定したインデックス位置にある要素。 </returns>
        /// <exception cref = "T:System.ArgumentOutOfRangeException">
        /// <paramref name = "index" /> が <see cref = "T:System.Collections.Generic.IList`1" /> の有効なインデックスではありません。
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        /// このプロパティが設定されていますが、
        /// <see cref = "T:System.Collections.Generic.IList`1" /> が読み取り専用です。
        /// </exception>
        public T this[int index]
        {
            get
            {
                if (index < _countList[0])
                {
                    return _self[index];
                }
                return _other[index - _countList[0]];
            }
            set { throw new NotImplementedException(); }
        }
    }

    public static class ReadOnlyMutliCollection
    {
        public static ReadOnlyMultiCollection<T> MultiMerge<T>(
            this IList<T> self, IList<T> other)
        {
            return new ReadOnlyMultiCollection<T>(self, other);
        }

        public static ReadOnlyMultiCollection<T> Merge<T>(
            params IList<T>[] arr)
        {
            ReadOnlyMultiCollection<T> self = arr[0].MultiMerge(arr[1]);
            for (int i = 2; i < arr.Length; i++)
            {
                self = new ReadOnlyMultiCollection<T>(self, arr[i]);
            }
            return self;
        }
    }
}
