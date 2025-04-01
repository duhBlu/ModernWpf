using ModernWpf.Controls;

namespace ModernWpf.SampleApp.ControlPages
{
    /// <summary>
    /// Interaction logic for ProgressRingPerfPage.xaml
    /// </summary>
    public partial class ProgressRingPerfPage
    {
        public ProgressRingPerfPage()
        {
            InitializeComponent();

            for (int i = 0; i < 300; i++)
            {
                panel.Children.Add(new ProgressRing { Width = 60, Height = 60, IsActive = true });
            }
        }
    }
}
