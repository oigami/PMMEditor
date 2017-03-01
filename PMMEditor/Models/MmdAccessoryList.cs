using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using PMMEditor.MMDFileParser;

namespace PMMEditor.Models
{
    public class MmdAccessoryList : NotificationObject
    {
        private readonly List<int> _drawOrder = new List<int>();

        public MmdAccessoryList()
        {
            List = new ObservableCollection<MmdAccessoryModel>();
        }

        #region NameList変更通知プロパティ

        public IEnumerable<string> NameList => List.Select(i => i.Name);

        public IEnumerable<string> NameEnglishList => List.Select(i => i.NameEnglish);

        #endregion

        public ObservableCollection<MmdAccessoryModel> List { get; }

        public async Task Set(IEnumerable<PmmStruct.AccessoryData> list)
        {
            List.Clear();
            var order = new SortedDictionary<int, int>();
            foreach (var item in list.Select((data, i) => new {data, i}))
            {
                var accessory = new MmdAccessoryModel();
                await accessory.Set(item.data);
                List.Add(accessory);
                order.Add(item.data.DrawOrder, item.i);
            }
            foreach (var i in order)
            {
                _drawOrder.Add(i.Value);
            }
        }
    }
}
