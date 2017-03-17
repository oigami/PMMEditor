using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public ObservableCollection<MmdModelModel> List { get; } = new ObservableCollection<MmdModelModel>();

        #region NameList変更通知プロパティ

        public IEnumerable<string> NameList => List.Select(i => i.Name);

        public IEnumerable<string> NameEnglishList => List.Select(i => i.NameEnglish);

        #endregion

        public void Delete(MmdModelModel model)
        {
            DispatcherHelper.UIDispatcher.Invoke(() => List.Remove(model));
        }

        public void Add(string path)
        {
            Task.Run(() =>
            {
                var model = new MmdModelModel(_logger);
                model.Set(path);
                if (model.IsInitialized)
                {
                    List.Add(model);
                }
            }).ContinueWith(t => _logger.Fatal("Unknown", t.Exception.InnerException),
                            TaskContinuationOptions.OnlyOnFaulted);
        }


        public async Task Set(IEnumerable<PmmStruct.ModelData> list)
        {
            await Task.Run(() =>
            {
                var order = new SortedDictionary<int, int>();
                List.Clear();
                foreach (var item in list.Select((data, i) => new { data, i }))
                {
                    var model = new MmdModelModel(_logger);
                    model.Set(item.data).Wait();
                    List.Add(model);
                    order.Add(item.data.DrawOrder, item.i);
                }
                foreach (var i in order)
                {
                    _drawOrder.Add(i.Value);
                }
            });
        }
    }
}
