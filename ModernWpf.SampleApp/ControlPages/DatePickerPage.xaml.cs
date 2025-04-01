using System.Windows;

namespace ModernWpf.SampleApp.ControlPages
{
    /// <summary>
    /// Interaction logic for DatePickerPage.xaml
    /// </summary>
    public partial class DatePickerPage
    {
        public DatePickerPage()
        {
            InitializeComponent();
        }

        private void BlackoutDatesInPast(object sender, RoutedEventArgs e)
        {
            datePicker.BlackoutDates.AddDatesInPast();
        }
    }
}
