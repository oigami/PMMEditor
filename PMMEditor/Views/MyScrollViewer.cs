using System.Windows.Controls;
using System.Windows.Media;

namespace PMMEditor.Views
{
    /*
     * ScrollViewerのイベントを下層のコントロールへ透過させる方法
     * http://qiita.com/takanemu/items/a82c8f4d68c7c31e7138
     */

    internal class MyScrollViewer : ScrollViewer
    {
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return null;
        }
    }
}
