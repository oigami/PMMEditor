using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PMMEditor.ECS;

namespace PMMEditor.Models.Thread
{
    public class ThreadQueue : IDisposable
    {

        private System.Threading.Thread _thread;
        private DispatcherTimer _timer;
        private Action _queue;
        public void PushQueue(Action func)
        {
            _queue = func;
        }

        public ThreadQueue()
        {
            _thread = new System.Threading.Thread(() =>
            {
                _timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher.CurrentDispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(1)
                };
                _timer.Tick += DispatcherTimer_Tick;
                // タイマーの実行開始
                _timer.Start();
                while (true)
                {
                    Dispatcher.Run();
                }
            })
            {
                Priority = ThreadPriority.Highest
            };
            _thread.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            Action action = _queue;
            action?.Invoke();
        }

        #region IDisposable Support
        private bool _disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _thread.Abort();
                    _timer.Stop();
                }


                _disposedValue = true;
            }
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
