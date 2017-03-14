using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Livet;
using PMMEditor.MMDFileParser;

namespace PMMEditor.Models
{
    public class MmdModelList : NotificationObject
    {
        private readonly List<int> _drawOrder = new List<int>();

        public ObservableCollection<MmdModelModel> List { get; } = new ObservableCollection<MmdModelModel>();

        #region NameList変更通知プロパティ

        public IEnumerable<string> NameList => List.Select(i => i.Name);

        public IEnumerable<string> NameEnglishList => List.Select(i => i.NameEnglish);

        #endregion

        public async Task Set(IEnumerable<PmmStruct.ModelData> list)
        {
            var order = new SortedDictionary<int, int>();
            List.Clear();
            foreach (var item in list.Select((data, i) => new { data, i }))
            {
                var model = new MmdModelModel();
                await model.Set(item.data);
                List.Add(model);
                order.Add(item.data.DrawOrder, item.i);
            }
            foreach (var i in order)
            {
                _drawOrder.Add(i.Value);
            }
        }
    }
}
