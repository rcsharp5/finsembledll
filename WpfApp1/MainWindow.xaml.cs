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
        private LinkerWindow linkerWindow;
        private SortedDictionary<string, Button> LinkerGroups = new SortedDictionary<string, Button>();
        private FinsembleBridge bridge;
        private string windowName;
        private Docking docking;

        public MainWindow(string FinsembleWindowName)
        {
            if (!string.IsNullOrEmpty(FinsembleWindowName))
            {
                windowName = FinsembleWindowName;
            }
            else
            {
                windowName = Guid.NewGuid().ToString();
            }
            
            bridge = new FinsembleBridge(new System.Version("8.56.28.34"));
            bridge.Connect();
            bridge.Connected += Bridge_Connected;
        }

        private void Bridge_Connected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                // Create the Linker Window. It needs a connected Openfin Bridge
                linkerWindow = new LinkerWindow(bridge);

                // Subscribe to Linker Publishes
                bridge.LinkerSubscribe += LinkerSubscriber;

                // Subscribe to topics
                bridge.SendRPCCommand(ChartIQ.Finsemble.Linker.Topic.Subscribe, "symbol", bridge.CallbackChannel.Subscribe);

                // Initialize this Window and show it
                InitializeComponent();
                this.Show();

                // Connect to Finsemble Docking
                docking = new Docking(bridge, this, windowName, windowName + "-channel");
            });
        }

        /**
         * Handle Snapping And Docking updates.
         */ 
        public void Docking_GroupUpdate(dynamic groups)
        {
            if (groups.dockingGroup != "")
            {
                Docking.Content = "@";
                Docking.Visibility = Visibility.Visible;
                Minimize.SetValue(Canvas.RightProperty, 105.0);
                Docking.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF039BFF"));
            }
            else if (groups.snappingGroup != "")
            {
                Docking.Content = ">";
                Docking.Visibility = Visibility.Visible;
                Minimize.SetValue(Canvas.RightProperty, 105.0);
                Docking.Background = Brushes.Transparent;
            }
            else
            {
                Docking.Visibility = Visibility.Hidden;
                Minimize.SetValue(Canvas.RightProperty, 70.0);
            }
            Window_Size_Changed();
        }

        /*
         * Linker Data Handler - TODO - not working
         */
        public void LinkerSubscriber(object sender, LinkerEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                SendData.Text = e.Message;
            });
        }

        /*
         * Let Docking Move this window instead of Windows - TODO - move this into docking to dynamically create these
         */
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

        /*
         * Maximize/Restore using Docking
         */
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

        /*
         * Minimize using docking
         */ 
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            docking.Minimize();
        }

        /*
         * Close cleanly
         */ 
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            linkerWindow.Close();
            docking.Close();
            this.Close();
        }

        /*
         * Show Linker
         */
        private void Linker_Click(object sender, RoutedEventArgs e)
        {
            linkerWindow.Left = this.Left;
            linkerWindow.Top = this.Top + 35;

            // BS Hack to get window to focus properly - https://stackoverflow.com/questions/21033262/force-window-to-have-focus-when-opened

            linkerWindow.Show();
            linkerWindow.Owner = this;
            linkerWindow.Activate();
            linkerWindow.Topmost = true;
            linkerWindow.Topmost = false;
            linkerWindow.Focus();
        }

        /*
         * Show Linker Pills
         */
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
                var style = this.Resources["LinkerPill"];
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
            Window_Size_Changed();

        }

        /*
         * Dock and undock
         */
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

        /*
         * Hover Color Changes
         */
        private void Window_GotFocus(object sender, EventArgs e)
        {
            Color color = (Color)ColorConverter.ConvertFromString("#FF133F7C");
            Toolbar.Background = new SolidColorBrush(color);
            var buttonStyle = this.Resources["ToolbarRegularButton"];
            var closeButtonStyle = this.Resources["ToolbarCloseButton"];
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

        /*
         * Handle when size of windows changes, linker/docking groups are joined left
         */
        private void Window_Size_Changed()
        {
            double LeftWidths = 35 + LinkerGroups.Count * 15;
            double RightWidths = 105;
            if (Docking.IsVisible) RightWidths = 140;
            Title.SetValue(Canvas.LeftProperty, LeftWidths);
            Title.Width = this.Width - LeftWidths - RightWidths;
        }

        /*
         * Catch restores for Docking - TODO - move this to docking.
         */
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if(this.WindowState == WindowState.Normal)
            {
                docking.Restore();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            bridge.SendRPCCommand(ChartIQ.Finsemble.Linker.Topic.Publish, new JObject { ["dataType"] = "symbol", ["data"] = SendData.Text });
        }
    }
}
