using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Linker LinkerWindow = new Linker();
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Frame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Maximized;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            LinkerWindow.Close();
            this.Close();
        }

        private void Linker_Click(object sender, RoutedEventArgs e)
        {
            LinkerWindow.Left = this.Left;
            LinkerWindow.Top = this.Top + 30;
            // BS Hack to get window to focus properly - https://stackoverflow.com/questions/21033262/force-window-to-have-focus-when-opened
            LinkerWindow.Show();
            LinkerWindow.Owner = this;
            LinkerWindow.Activate();
            LinkerWindow.Focus();
            LinkerWindow.Topmost = true;
            LinkerWindow.Topmost = false;
        }
    }
}
