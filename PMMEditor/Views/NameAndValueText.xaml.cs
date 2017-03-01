using System.Windows;

namespace PMMEditor.Views
{
    /// <summary>
    /// NameAndValueText.xaml の相互作用ロジック
    /// </summary>
    public partial class NameAndValueText
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
                typeof(double),
                typeof(NameAndValueText),
                new PropertyMetadata(double.NaN));

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

        public double ValueWidth
        {
            get { return (double) GetValue(ValueWidthProperty); }
            set { SetValue(ValueWidthProperty, value); }
        }
    }
}
