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

        private bool _isPlaying;

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
            if (_isPlaying)
            {
                return;
            }
            NowFrame++;
        }

        public void PrevFrame()
        {
            if (_isPlaying)
            {
                return;
            }
            if (NowFrame > 0)
            {
                NowFrame--;
            }
        }

        public void Play()
        {
            _isPlaying = false;
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        public void SwitchPlayAndStop()
        {
            _isPlaying = !_isPlaying;
        }
    }
}
