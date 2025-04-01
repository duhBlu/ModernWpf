using System.Windows;

namespace ModernWpf.SampleApp.ControlPages
{
    public partial class HyperlinkButtonPage
    {
        public HyperlinkButtonPage()
        {
            InitializeComponent();
        }

        private void GoToHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ListBoxPage));
        }
    }
}
