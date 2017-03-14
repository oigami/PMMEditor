using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace PMMEditor.Views
{
    public class ByteArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bytes = value as byte[];
            if (bytes == null)
            {
                throw new ArgumentException("value is not byte[]", nameof(value));
            }
            return string.Join(",", Array.ConvertAll(bytes, o => $"{o,3}"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /*
     * ViewModelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedWeakEventListenerや
     * CollectionChangedWeakEventListenerを使うと便利です。独自イベントの場合はLivetWeakEventListenerが使用できます。
     * クローズ時などに、LivetCompositeDisposableに格納した各種イベントリスナをDisposeする事でイベントハンドラの開放が容易に行えます。
     *
     * WeakEventListenerなので明示的に開放せずともメモリリークは起こしませんが、できる限り明示的に開放するようにしましょう。
     */

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            // キーボードショートカットなどに対応するためEnterを押したらフォーカスを強制的に外す
            KeyDown += (sender, e) =>
            {
                if (e.Key != Key.Enter)
                {
                    return;
                }
                Keyboard.ClearFocus();
                FocusManager.SetFocusedElement(this, this);
            };
        }
    }
}
