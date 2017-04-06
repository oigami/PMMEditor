using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using PMMEditor.Log;
using PMMEditor.MMDFileParser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Action = System.Action;

namespace PMMEditor.Models
{
    public class MmdModelList : NotificationObject
    {
        private readonly List<int> _drawOrder = new List<int>();
        private readonly ILogger _logger;

        public MmdModelList(ILogger logger)
        {
            _logger = logger;
        }

        private readonly ObservableCollection<MmdModelModel> _list = new ObservableCollection<MmdModelModel>();
        private ReadOnlyObservableCollection<MmdModelModel> _readOnlyList;

        public ReadOnlyObservableCollection<MmdModelModel> List =>
            _readOnlyList ?? (_readOnlyList = new ReadOnlyObservableCollection<MmdModelModel>(_list));

        #region NameList変更通知プロパティ

        public IEnumerable<string> NameList => List.Select(i => i.Name);

        public IEnumerable<string> NameEnglishList => List.Select(i => i.NameEnglish);

        #endregion

        public void Clear()
        {
            _tokenSource?.Cancel();
            _tokenSource = null;
            _list.Clear();
        }

        public void Delete(MmdModelModel model)
        {
            _list.Remove(model);
        }

        public void Add(string path)
        {
            Task.Run(() =>
            {
                var model = new MmdModelModel(_logger);
                model.Set(path);
                if (model.IsInitialized)
                {
                    _list.Add(model);
                }
            }).ContinueOnlyOnFaultedErrorLog(_logger);
        }

        private CancellationTokenSource _tokenSource;
        public void Set(IEnumerable<PmmStruct.ModelData> list)
        {
            DispatcherHelper.UIDispatcher.Invoke(() =>
            {
                if (_tokenSource != null)
                {
                    throw new InvalidOperationException("すでにセット処理が実行中です");
                }

                _tokenSource = new CancellationTokenSource();
            });
            CancellationToken token = _tokenSource.Token;
            Task.Run(async () =>
            {
                var order = new SortedDictionary<int, int>();
                _list.Clear();
                foreach (var item in list.Select((data, i) => new { data, i }))
                {
                    var model = new MmdModelModel(_logger);
                    await model.SetAsync(item.data).ConfigureAwait(false);
                    _list.Add(model);
                    order.Add(item.data.DrawOrder, item.i);
                }
                foreach (var i in order)
                {
                    _drawOrder.Add(i.Value);
                }

                DispatcherHelper.UIDispatcher.Invoke(() => _tokenSource = null);
            }, token).ContinueOnlyOnFaultedErrorLog(_logger, "Charactor List Set error", () => _list.Clear());
        }
    }
}
