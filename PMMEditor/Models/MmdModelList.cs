using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using PMMEditor.ECS;
using PMMEditor.Log;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.Graphics;
using PMMEditor.Models.MMDModel;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Action = System.Action;

namespace PMMEditor.Models
{
    public class MmdModelList : NotificationObject
    {
        private readonly List<int> _drawOrder = new List<int>();
        private readonly ILogger _logger;
        private readonly object _syncObject = new object();

        public MmdModelList(ILogger logger)
        {
            _logger = logger;
        }

        private readonly ObservableCollection<Entity> _list = new ObservableCollection<Entity>();
        private ReadOnlyObservableCollection<Entity> _readOnlyList;

        public ReadOnlyObservableCollection<Entity> List =>
            _readOnlyList ?? (_readOnlyList = new ReadOnlyObservableCollection<Entity>(_list));

        #region NameList変更通知プロパティ

        public IEnumerable<string> NameList => List.Select(i => i.GetComponent<MmdModelModel>().Name);

        public IEnumerable<string> NameEnglishList => List.Select(i => i.GetComponent<MmdModelModel>().NameEnglish);

        #endregion

        public void Clear()
        {
            _tokenSource?.Cancel();
            _tokenSource = null;
            _list.Clear();
        }

        public void Delete(Entity model)
        {
            _list.Remove(model);
        }

        public void Add(string path)
        {
            try
            {
                var blob = new FileBlob(path);
                Add(blob);
            }
            catch (Exception ex)
            {
                _logger.Error("", ex);
            }
        }

        public void Add(FileBlob blob)
        {
            Task.Run(() =>
            {
                Entity entity = Model.System.CreateEntity();
                MmdModelModel model = entity.AddComponent<MmdModelModel>();
                model.Initialize(_logger);
                model.Set(blob);
                MmdModelRendererSource rendererSource = entity.AddComponent<MmdModelRendererSource>();
                rendererSource.Initialize(_logger, GraphicsModel.Device);

                if (model.IsInitialized)
                {
                    lock (_syncObject)
                    {
                        _list.Add(entity);
                    }
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
            Task.Run(() =>
           {
               var order = new SortedDictionary<int, int>();
               _list.Clear();
               foreach (var item in list.Select((data, i) => new { data, i }))
               {
                   Entity entity = Model.System.CreateEntity();
                   MmdModelModel model = entity.AddComponent<MmdModelModel>();
                   model.Initialize(_logger);
                   model.Set(new FileBlob(item.data.Path), item.data);
                   MmdModelRendererSource rendererSource = entity.AddComponent<MmdModelRendererSource>();
                   rendererSource.Initialize(_logger, GraphicsModel.Device);
                   _list.Add(entity);
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
