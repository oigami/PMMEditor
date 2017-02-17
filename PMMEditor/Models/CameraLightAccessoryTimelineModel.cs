using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Xceed.Wpf.Toolkit;

namespace PMMEditor.Models
{
    internal class CameraLightAccessoryTimelineModel : NotificationObject, IDisposable
    {
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();

        public ReadOnlyReactiveCollection<KeyFrameList<MmdAccessoryModel.BoneKeyFrame>> AccessoryKeyFrameLists { get; }

        public CameraLightAccessoryTimelineModel(Model model)
        {
            AccessoryKeyFrameLists =
                model.MmdAccessoryList.List.ToReadOnlyReactiveCollection(i => i.BoneKeyList[0]).AddTo(Disposable);
            AccessoryKeyFrameLists.ObserveElementProperty(i => i.MaxFrame)
                                  .Subscribe(i => MaxFrameIndex = Math.Max(MaxFrameIndex, i.Value)).AddTo(Disposable);
            foreach (var item in AccessoryKeyFrameLists)
            {
                MaxFrameIndex = Math.Max(MaxFrameIndex, item.MaxFrame);
            }

            AccessoryKeyFrameLists
                .ObserveAddChanged()
                .Subscribe(item =>
                {
                    item.ObserveProperty(i=>i.MaxFrame).Subscribe(i => MaxFrameIndex = Math.Max(MaxFrameIndex, i)).AddTo(Disposable);
                    foreach (var boneKeyFrame in item)
                    {
                        MaxFrameIndex = Math.Max(boneKeyFrame.Key, MaxFrameIndex);
                     }
                }).AddTo(Disposable);
        }

        #region MaxFrameIndex変更通知プロパティ

        private int _MaxFrameIndex;

        public int MaxFrameIndex
        {
            get { return _MaxFrameIndex; }
            set
            {
                if (_MaxFrameIndex == value)
                {
                    return;
                }
                _MaxFrameIndex = value;
                RaisePropertyChanged();
            }
        }

        /// <summary> アンマネージ リソースの解放またはリセットに関連付けられているアプリケーション定義のタスクを実行します。 </summary>
        public void Dispose()
        {
            Disposable.Dispose();
        }

        #endregion
    }
}
