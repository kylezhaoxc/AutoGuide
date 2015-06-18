using System.Windows;

namespace Auto_Guide
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var rt = new NewRoute();
            rt.Show();
            Hide();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var gd = new Guide();
            gd.Show();
            Hide();
        }
    }
}