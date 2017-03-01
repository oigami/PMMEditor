using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Livet;
using Livet.Behaviors.ControlBinding.OneWay;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using PMMEditor.ViewModels.Panes;

namespace PMMEditor.ViewModels.Panes
{
    internal class TimelineTranslateViewModel : PaneViewModelBase
    {
        private readonly Model _model;

        public override string Title => "TimelineTranslate";

        public override string ContentId { get; } = typeof(TimelineTranslateViewModel).FullName;


        public TimelineTranslateViewModel(Model model)
        {
            _model = model;
        }

        #region Width変更通知プロパティ

        private double _Width;

        public double Width
        {
            get { return _Width; }
            set
            {
                if (_Width == value)
                {
                    return;
                }
                _Width = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        private IEnumerable<PmmStruct.ModelData.BoneInitFrame> CreateList(int num)
        {
            var item = _model?.PmmStruct?.ModelDatas?[0].BoneInitFrames?[num];
            if (item == null)
            {
                yield break;
            }
            yield return item;
            int nowIndex = item.NextIndex;
            var boneFrameList = _model?.PmmStruct?.ModelDatas?[0].BoneKeyFrames;
            while (nowIndex != 0)
            {
                var tmp = boneFrameList.FirstOrDefault(frame => frame.DataIndex == nowIndex);
                if (tmp == null)
                {
                    yield break;
                }
                yield return tmp;
                nowIndex = tmp.NextIndex;
            }
        }

        public List<string> BoneList => _model?.PmmStruct?.ModelDatas?[0].BoneName;

        public List<IEnumerable<PmmStruct.ModelData.BoneInitFrame>> MyList
        {
            get
            {
                List<IEnumerable<PmmStruct.ModelData.BoneInitFrame>> list =
                    new List<IEnumerable<PmmStruct.ModelData.BoneInitFrame>>();
                for (int i = 0; i < _model?.PmmStruct?.ModelDatas?[0].BoneInitFrames?.Count; i++)
                {
                    list.Add(CreateList(i));
                }
                return list;
            }
        }

        public double MaxFrameNumber => (_model?.PmmStruct?.ModelDatas?[0].LastFrame ?? 0);
    }
}
