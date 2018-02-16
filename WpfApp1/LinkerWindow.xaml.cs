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
using System.Windows.Shapes;
using ChartIQ.Finsemble;
using Newtonsoft.Json.Linq;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Linker.xaml
    /// </summary>
    public partial class LinkerWindow : Window
    {
        private FinsembleBridge bridge;
        public LinkerWindow()
        {
            bridge = new FinsembleBridge(new System.Version("8.56.28.34"));
            bridge.Connect();           
            bridge.Connected += Bridge_Connected;
            InitializeComponent();
        }

        private void Bridge_Connected(object sender, EventArgs e)
        {
            string topic = Linker.Topic.GetAllGroups;
            bridge.SubscribeToChannel("allGroupsChannel", GotAllGroups);
            bridge.SendRPCCommand(topic, new JObject().ToString(), "allGroupsChannel");
        }

        private void group_Click(object sender, RoutedEventArgs e)
        {
            // TODO - Why does this not work on first click???
            var sendingButton = (System.Windows.Controls.Button)sender;
            string topic;
            var owner = (MainWindow)this.Owner;
            if (sendingButton.Content.ToString() == "r")
            {
                topic = Linker.Topic.RemoveFromGroup;
                sendingButton.Content = "";
                owner.Groups_Changed(sendingButton.Name, sendingButton.Background, false);
            }
            else
            {
                topic = Linker.Topic.AddToGroup;
                sendingButton.Content = "r";
                owner.Groups_Changed(sendingButton.Name, sendingButton.Background, true);
            }
            bridge.SendRPCCommand(topic, sendingButton.Name);
            this.Hide();
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void GotAllGroups(string sourceUuid, string topic, object message)
        {
            var joMessage = message as JObject;
            var allGroups = joMessage.GetValue("0");
            double baseLocation = 8;
            foreach (var g in allGroups)
            {
                var group = g as JObject;
                var name = group.GetValue("name").ToString();
                var colorcode = group.GetValue("color").ToString();
                Application.Current.Dispatcher.Invoke((Action)delegate {
                    createButton(name, colorcode, baseLocation);
                });
                baseLocation += 24;
            }
        }

        public void createButton(string name, string colorcode, double topMargin)
        {
            colorcode = "#FF" + colorcode.Replace("#", "");
            Color color = (Color)ColorConverter.ConvertFromString(colorcode);

            var button = new Button();
            button.Name = name;
            button.Background = new SolidColorBrush(color);
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.Margin = new Thickness(0, topMargin, 0, 0);
            button.VerticalAlignment = VerticalAlignment.Top;
            button.Width = 50;
            button.Height = 20;
            button.Click += group_Click;
            var style = this.Resources["RoundedButtonStyle"];
            button.SetValue(StyleProperty, style);
            button.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./resources/#finfont");
            button.Content = "";
            MainGrid.Children.Add(button);
        }

        public void Subscribe(EventHandler<LinkerEventArgs> h)
        {
            bridge.LinkerSubscribe += h;
        }
    }
}
