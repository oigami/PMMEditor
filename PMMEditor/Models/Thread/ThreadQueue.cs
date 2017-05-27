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

        private readonly CancellationTokenSource _cancelToken;
        private DispatcherTimer _timer;
        private Action _queue;
        public void PushQueue(Action func)
        {
            _queue = func;
        }

        public ThreadQueue()
        {
            _cancelToken = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
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
            }, _cancelToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            _queue?.Invoke();
        }

        #region IDisposable Support
        private bool _disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _cancelToken.Cancel();
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
