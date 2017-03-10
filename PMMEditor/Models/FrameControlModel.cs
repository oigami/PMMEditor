using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using PMMEditor.MVVM;

namespace PMMEditor.Models
{
    public class FrameControlModel : BindableBase
    {
        public FrameControlModel()
        {
            CompositionTarget.Rendering += CompositionTarget_FrameUpdate;
        }

        #region NowFrame変更通知プロパティ

        private int _nowFrame;

        public int NowFrame
        {
            get { return _nowFrame; }
            set { SetProperty(ref _nowFrame, value); }
        }

        #endregion

        private readonly bool _isPlaying = false;

        private void CompositionTarget_FrameUpdate(object sender, EventArgs e)
        {
            if (_isPlaying == false)
            {
                return;
            }
            NowFrame++;
        }

        public void NextFrame()
        {
            NowFrame++;
        }

        public void PrevFrame()
        {
            if (NowFrame > 0)
            {
                NowFrame--;
            }
        }
    }
}
