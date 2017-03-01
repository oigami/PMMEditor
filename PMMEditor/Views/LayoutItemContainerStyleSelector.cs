using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Markup;

namespace PMMEditor.Views
{
    [ContentProperty("Items")]
    public class LayoutItemContainerStyleSelector : StyleSelector
    {
        public class ItemList : Collection<LayoutItemTypedStyle> {}

        public ItemList Items { get; } = new ItemList();

        public override Style SelectStyle(object item, DependencyObject container)
        {
            var styleData = Items.FirstOrDefault(s => item.GetType().IsSubclassOf(s.DataType));
            return styleData?.Style ?? base.SelectStyle(item, container);
        }
    }

    [ContentProperty("Style")]
    public class LayoutItemTypedStyle
    {
        public Type DataType { get; set; }

        public Style Style { get; set; }
    }
}
