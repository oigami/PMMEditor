using Livet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PMMEditor.Views.Documents
{
    /// <summary>
    /// TimelineView.xaml の相互作用ロジック
    /// </summary>
    public partial class TimelineView : UserControl
    {
        public TimelineView()
        {
            InitializeComponent();
            CreateGridLine();
        }

        void CreateGridLine()
        {
            var dateTemplate = (DataTemplate) Resources["GridBackground"];
            var gridBackground = dateTemplate.LoadContent() as FrameworkElement;
            // 計測、配置は自前でやらないとRenderされないので注意
            gridBackground.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            gridBackground.Arrange(new Rect(0, 0, gridBackground.DesiredSize.Width, gridBackground.DesiredSize.Height));
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int) gridBackground.ActualWidth, (int) gridBackground.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(gridBackground);
            bitmap.Freeze();
            Image.ImageSource = bitmap;
        }
    }
}
