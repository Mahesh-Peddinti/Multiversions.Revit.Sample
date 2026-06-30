using Multiversions.Revit.Sample.ViewModels;
using System.Windows;
using System.Windows.Media;


namespace Multiversions.Revit.Sample.Views
{
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }
        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Logic to switch your app's background to a Flat Dark color (e.g., #121212)
            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Logic to switch your app's background back to a Flat Light color (e.g., #FFFFFF)
            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        }
    }
}
