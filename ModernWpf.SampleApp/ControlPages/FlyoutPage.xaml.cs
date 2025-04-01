using ModernWpf.Controls;
using System.Windows;

namespace ModernWpf.SampleApp.ControlPages
{
    public partial class FlyoutPage
    {
        public FlyoutPage()
        {
            InitializeComponent();
        }

        private void DeleteConfirmation_Click(object sender, RoutedEventArgs e)
        {
            Flyout f = FlyoutService.GetFlyout(Control1) as Flyout;
            if (f != null)
            {
                f.Hide();
            }
        }
    }
}
