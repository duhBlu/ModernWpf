using System.Windows;

namespace ModernWpf.Controls
{
    public class ListViewItem : ListViewBaseItem
    {
        static ListViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListViewItem), new FrameworkPropertyMetadata(typeof(ListViewItem)));
        }
    }
}
