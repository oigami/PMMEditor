using System.Windows;
using System.Windows.Controls;

namespace PMMEditor.Views
{
    /// <summary>
    /// NameAndValueText.xaml の相互作用ロジック
    /// </summary>
    public partial class NameAndValueText : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                                        nameof(Value),
                                        typeof(string),
                                        typeof(NameAndValueText),
                                        new PropertyMetadata(null));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                                        nameof(Text),
                                        typeof(string),
                                        typeof(NameAndValueText),
                                        new PropertyMetadata(null));

        public static readonly DependencyProperty ValueWidthProperty =
            DependencyProperty.Register(
                                        nameof(ValueWidth),
                                        typeof(int),
                                        typeof(NameAndValueText),
                                        new PropertyMetadata(0));

        public NameAndValueText()
        {
            InitializeComponent();
        }

        public string Value
        {
            get { return (string) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public int ValueWidth
        {
            get { return (int) GetValue(ValueWidthProperty); }
            set { SetValue(ValueWidthProperty, value); }
        }
    }
}
