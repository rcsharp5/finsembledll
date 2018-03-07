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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WpfApp1
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private LinkerWindow linkerWindow;
        private SortedDictionary<string, Button> LinkerGroups = new SortedDictionary<string, Button>();
        private FinsembleBridge bridge;
        private string windowName;
        private string componentType = "Unknown";
        private bool sendCloseToFinsemble = true;
        private string top, left, height, width;

        public MainWindow(string FinsembleWindowName, string componentType, string top, string left, string height, string width)
        {
            if (!string.IsNullOrEmpty(FinsembleWindowName))
            {
                windowName = FinsembleWindowName;
            }
            else
            {
                windowName = Guid.NewGuid().ToString();
            }

            if (!string.IsNullOrEmpty(componentType))
            {
                this.componentType = componentType;
            }

            this.top = top;
            this.left = left;
            this.height = height;
            this.width = width;

            bridge = new FinsembleBridge(new System.Version("8.56.28.34"), windowName, componentType, this);
            bridge.Connect();
            bridge.Connected += Bridge_Connected;
        }

        private void Bridge_Connected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                bridge.linkerClient.subscribe("symbol", (EventHandler<FinsembleEventArgs>)delegate(object s, FinsembleEventArgs args)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                    {
                        SendData.Text = args.response["data"].ToString();
                    });
                });

                // Initialize this Window and show it
                InitializeComponent();
                if (!string.IsNullOrEmpty(top))
                {
                    this.Top = Double.Parse(top);
                }

                if (!string.IsNullOrEmpty(left))
                {
                    this.Left = Double.Parse(left);
                }

                if (!string.IsNullOrEmpty(height))
                {
                    this.Height = Double.Parse(height);
                }

                if (!string.IsNullOrEmpty(width))
                {
                    this.Width = Double.Parse(width);
                }
                this.Show();
                bridge.docking.DockingGroupUpdateHandler += Docking_GroupUpdate;

                // router test
                bridge.routerClient.addListener("test", FinsembleListener);
                
            });
        }



        public void FinsembleListener(object sender, FinsembleEventArgs message)
        {
            if (message.error != null)
            {
                dynamic error = JsonConvert.DeserializeObject<ExpandoObject>(message.error.ToString(), new ExpandoObjectConverter());
            }
            else
            {
                dynamic response = JsonConvert.DeserializeObject<ExpandoObject>(message.response.ToString(), new ExpandoObjectConverter());

                var data = response.data;
                foreach (KeyValuePair<string, object> kvp in data)
                {

                }
            }
        }

        /**
         * Handle Snapping And Docking updates.
         */
        public void Docking_GroupUpdate(object sender, dynamic groups)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
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
            });
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
            bridge.docking.StartMove(sender, e);
        }

        private void Toolbar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bridge.docking.EndMove(sender, e);
        }

        private void Toolbar_MouseMove(object sender, MouseEventArgs e)
        {
            bridge.docking.Move(sender, e);
        }

        /*
         * Maximize/Restore using Docking
         */
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var senderButton = (System.Windows.Controls.Button)sender;
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                bridge.docking.Restore();
                senderButton.Content = "3";
            }
            else
            {
                bridge.docking.Maxmimize();
                senderButton.Content = "#";
            }
        }

        /*
         * Minimize using docking
         */
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            bridge.docking.Minimize();
        }

        /*
         * Close cleanly
         */
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /*
         * Show Linker
         */
        private void Linker_Click(object sender, RoutedEventArgs e)
        {
            bridge.linkerClient.showLinkerWindow();
            bridge.linkerClient.onStateChange((EventHandler<FinsembleEventArgs>)delegate (object sender2, FinsembleEventArgs args)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                {
                    var channels = args.response["channels"] as JArray;
                    var allChannels = args.response["allChannels"] as JArray;

                    // Hide all LinkerGroups
                    foreach (var item in LinkerGroups)
                    {
                        item.Value.Visibility = Visibility.Hidden;
                    }

                    // Loop through Channels
                    Double baseLeft = 36.0;
                    Double increment = 15;
                    foreach (JObject item in allChannels)
                    {
                        var groupName = (string)item["name"];
                        // check if in this group
                        if (channels.Where(jt => jt.Value<string>() == groupName).Count() > 0)
                        {
                            if (!LinkerGroups.ContainsKey(groupName))
                            {
                                var groupRectangle = new Button();
                                groupRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                                groupRectangle.VerticalAlignment = VerticalAlignment.Top;
                                groupRectangle.Width = 10;
                                groupRectangle.Height = 25;
                                groupRectangle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)item["color"]));
                                Toolbar.Children.Add(groupRectangle);
                                groupRectangle.SetValue(Canvas.TopProperty, 5.0);
                                groupRectangle.Name = groupName;
                                var style = this.Resources["LinkerPill"];
                                groupRectangle.SetValue(StyleProperty, style);
                                LinkerGroups[groupName] = groupRectangle;
                            }
                            LinkerGroups[groupName].SetValue(Canvas.LeftProperty, baseLeft);
                            baseLeft += increment;
                            LinkerGroups[groupName].Visibility = Visibility.Visible;
                        }
                    }
                    Window_Size_Changed();
                });



            });
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
                bridge.docking.LeaveGroup(sender, e);
            }
            else
            {
                bridge.docking.FormGroup(sender, e);
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
            int LinkerGroupCount = LinkerGroups.Where(g => g.Value.Visibility == Visibility.Visible).Count();
            double LeftWidths = 35 + LinkerGroupCount * 15;
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
            if (this.WindowState == WindowState.Normal)
            {
                bridge.docking.Restore();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            bridge.linkerClient.publish(new JObject {
                ["dataType"] = "symbol",
                ["data"] = SendData.Text
            });
        }

        private void RouterTransmit_Click(object sender, RoutedEventArgs e)
        {
            //bridge.routerClient.transmit("test", new JObject { ["hello"] = "hello" });
            //EventHandler<FinsembleEventArgs> handler = (EventHandler<FinsembleEventArgs>)delegate (object s, FinsembleEventArgs ea) { };
            //bridge.routerClient.query("test", new JObject { ["hello"] = "hello" }, new JObject { }, handler);
            //bridge.distributedStoreClient.getStore(new JObject { ["store"] = "Finsemble_Linker", ["global"] = true }, handler);

        }
    }
}
