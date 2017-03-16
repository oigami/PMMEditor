using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace PMMEditor.Views.MMW
{
    [ContentProperty("Items")]
    public class LayoutItemTemplateSelector : DataTemplateSelector
    {
        public class ItemList : Collection<DataTemplate> {}

        public ItemList Items { get; } = new ItemList();

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var template = Items.FirstOrDefault(dt => item.GetType().Equals(dt.DataType));
            return template ?? base.SelectTemplate(item, container);
        }
    }
}
