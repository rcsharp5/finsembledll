using ChartIQ.Finsemble;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LinkerWindow LinkerWindow = new LinkerWindow();
        private SortedDictionary<string, Button> LinkerGroups = new SortedDictionary<string, Button>();
        private FinsembleBridge bridge;
        private string windowName;
        private Docking docking;

        public MainWindow(string FinsembleWindowName)
        {
            if (FinsembleWindowName != "")
            {
                windowName = FinsembleWindowName;
            }
            else
            {
                windowName = Guid.NewGuid().ToString();
            }
            LinkerWindow.Subscribe(LinkerSubscriber);
            bridge = new FinsembleBridge(new System.Version("8.56.28.34"));
            bridge.Connect();
            bridge.Connected += Bridge_Connected;
        }

        private void Bridge_Connected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                InitializeComponent();
                this.Show();
                docking = new Docking(bridge);
                docking.Register(this, windowName, windowName + "-channel");
            });
        }

        public void Docking_GroupUpdate(dynamic groups)
        {
            if(groups.dockingGroup != "")
            {
                Docking.Content = "@";
                Docking.Visibility = Visibility.Visible;
            } else if(groups.snappingGroup != "")
            {
                Docking.Content = ">";
                Docking.Visibility = Visibility.Visible;
            } else
            {
                Docking.Visibility = Visibility.Hidden;
            }
        }

        public void LinkerSubscriber(object sender, ChartIQ.Finsemble.LinkerEventArgs e)
        {
            MessageBox.Show(e.Message);
        }

        private void Toolbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            docking.StartMove(sender, e);
        }


        private void Toolbar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            docking.EndMove(sender, e);
        }


        private void Toolbar_MouseMove(object sender, MouseEventArgs e)
        {
            docking.Move(sender, e);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var senderButton = (System.Windows.Controls.Button)sender;
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                docking.Restore();
                senderButton.Content = "3";
            }
            else
            {
                docking.Maxmimize();
                senderButton.Content = "#";
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            docking.Minimize();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            LinkerWindow.Close();
            docking.Close();
            this.Close();
        }

        private void Linker_Click(object sender, RoutedEventArgs e)
        {
            LinkerWindow.Left = this.Left;
            LinkerWindow.Top = this.Top + 35;

            // BS Hack to get window to focus properly - https://stackoverflow.com/questions/21033262/force-window-to-have-focus-when-opened

            LinkerWindow.Show();
            LinkerWindow.Owner = this;
            LinkerWindow.Activate();
            LinkerWindow.Topmost = true;
            LinkerWindow.Topmost = false;
            LinkerWindow.Focus();
        }

        public void Groups_Changed(string group, Brush background, bool join)
        {
            if (!LinkerGroups.ContainsKey(group))
            {
                var groupRectangle = new Button();
                groupRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                groupRectangle.VerticalAlignment = VerticalAlignment.Top;
                groupRectangle.Width = 10;
                groupRectangle.Height = 25;
                groupRectangle.Background = background;
                Toolbar.Children.Add(groupRectangle);
                groupRectangle.SetValue(Canvas.TopProperty, 5.0);
                groupRectangle.Name = group;
                var style = this.Resources["RoundedButtonStyle"];
                groupRectangle.SetValue(StyleProperty, style);

                LinkerGroups[group] = groupRectangle;

            }
            if (!join)
            {
                LinkerGroups[group].Visibility = Visibility.Hidden;
            }
            else
            {
                LinkerGroups[group].Visibility = Visibility.Visible;
            }

            Double baseLeft = 36.0;
            Double increment = 15;

            foreach (var item in LinkerGroups)
            {
                if (item.Value.Visibility == Visibility.Visible)
                {
                    item.Value.SetValue(Canvas.LeftProperty, baseLeft);
                    baseLeft += increment;
                }
            }

        }


        private void Window_LocationChanged(object sender, EventArgs e)
        {
            T1.Text = this.Top + " " + this.Left;
        }

        private void Docking_Click(object sender, RoutedEventArgs e)
        {
            if (Docking.Content == "@")
            {
                docking.LeaveGroup(sender, e);
            }
            else
            {
                docking.FormGroup(sender, e);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void Window_GotFocus(object sender, EventArgs e)
        {
            Color color = (Color)ColorConverter.ConvertFromString("#FF133F7C");
            Toolbar.Background = new SolidColorBrush(color);
            var buttonStyle = this.Resources["ToolbarRegularButton"];
            var closeButtonStyle =  this.Resources["ToolbarCloseButton"];
            Minimize.SetValue(StyleProperty, buttonStyle);
            Maximize.SetValue(StyleProperty, buttonStyle);
            Linker.SetValue(StyleProperty, buttonStyle);
            Docking.SetValue(StyleProperty, buttonStyle);
            Close.SetValue(StyleProperty, closeButtonStyle);
        }

        private void Window_LostFocus(object sender, EventArgs e)
        {
            Color color = (Color)ColorConverter.ConvertFromString("#FF233958");
            Toolbar.Background = new SolidColorBrush(color);
            var buttonStyle = this.Resources["InactiveToolbarRegularButton"];
            var closeButtonStyle = this.Resources["InactiveToolbarCloseButton"];
            Minimize.SetValue(StyleProperty, buttonStyle);
            Maximize.SetValue(StyleProperty, buttonStyle);
            Linker.SetValue(StyleProperty, buttonStyle);
            Docking.SetValue(StyleProperty, buttonStyle);
            Close.SetValue(StyleProperty, closeButtonStyle);

        }
    }
}
