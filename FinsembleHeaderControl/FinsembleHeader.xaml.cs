using ChartIQ.Finsemble;
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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FinsembleHeaderControl
{
    /// <summary>
    /// Interaction logic for FinsembleHeader.xaml
    /// </summary>
    public partial class FinsembleHeader : UserControl
    {
        public FinsembleBridge bridge;
        private SortedDictionary<string, Button> LinkerGroups = new SortedDictionary<string, Button>();

        public FinsembleHeader()
        {
            InitializeComponent();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                bridge.window.WindowState = WindowState.Minimized;
            });
        }

        private void Toolbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bridge.docking.StartMove(sender, e);
        }

        private void Toolbar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bridge.docking.EndMove(sender, e);
        }

        private void Toolbar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            bridge.docking.Move(sender, e);
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            var senderButton = (System.Windows.Controls.Button)sender;
            if (bridge.window.WindowState == System.Windows.WindowState.Maximized)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                {
                    bridge.window.WindowState = WindowState.Normal;
                });
                senderButton.Content = "3";
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                {
                    bridge.window.WindowState = WindowState.Maximized;
                });
                senderButton.Content = "#";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            bridge.window.Close();
        }

        private void Linker_Click(object sender, RoutedEventArgs e)
        {
            bridge.linkerClient.ShowLinkerWindow();
            bridge.linkerClient.OnStateChange((EventHandler<FinsembleEventArgs>)delegate (object sender2, FinsembleEventArgs args)
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

        private void Window_Size_Changed()
        {
            int LinkerGroupCount = LinkerGroups.Where(g => g.Value.Visibility == Visibility.Visible).Count();
            double LeftWidths = 35 + LinkerGroupCount * 15;
            double RightWidths = 105;
            if (Docking.IsVisible) RightWidths = 140;
            Title.SetValue(Canvas.LeftProperty, LeftWidths);
            Title.Width = this.Width - LeftWidths - RightWidths;
        }

        private void Docking_Click(object sender, RoutedEventArgs e)
        {
            if (Docking.Content == "@")
            {
                bridge.docking.LeaveGroup();
            }
            else
            {
                bridge.docking.FormGroup();
            }
        }
    }
}
