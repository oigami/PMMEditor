using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using PMMEditor.MMDFileParser;

namespace PMMEditor.Models
{
    public class MmdModelList : NotificationObject
    {
        private readonly List<MmdModelModel> _modelList = new List<MmdModelModel>();
        private readonly List<int> _drawOrder = new List<int>();

        #region NameList変更通知プロパティ

        public IEnumerable<string> NameList => _modelList.Select(i => i.Name);

        public IEnumerable<string> NameEnglishList => _modelList.Select(i => i.NameEnglish);

        #endregion

        public async Task Set(IEnumerable<PmmStruct.ModelData> list)
        {
            var order = new SortedDictionary<int, int>();
            foreach (var item in list.Select((data, i) => new {data, i}))
            {
                var model = new MmdModelModel();
                await model.Set(item.data);
                _modelList.Add(model);
                order.Add(item.data.DrawOrder, item.i);
            }
            foreach (var i in order)
            {
                _drawOrder.Add(i.Value);
            }
        }
    }
}
