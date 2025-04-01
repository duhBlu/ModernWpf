using System.Windows;

namespace ModernWpf.Controls
{
    public class GridViewItem : ListViewBaseItem
    {
        static GridViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridViewItem), new FrameworkPropertyMetadata(typeof(GridViewItem)));
        }
    }
}
