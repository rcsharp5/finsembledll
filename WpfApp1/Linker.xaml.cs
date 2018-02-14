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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Linker.xaml
    /// </summary>
    public partial class Linker : Window
    {
        private FinsembleBridge bridge;
        public Linker()
        {
            bridge = new FinsembleBridge(new System.Version("8.56.28.34"));
            bridge.Connect();
            InitializeComponent();
        }

        private void group_Click(object sender, RoutedEventArgs e)
        {
            // TODO - Why does this not work on first click???
            var sendingButton = (System.Windows.Controls.Button)sender;
            string topic;
            if (sendingButton.Content.ToString() == "r")
            {
                topic = LinkerTopic.RemoveFromGroup;
                sendingButton.Content = "";
            }
            else
            {
                topic = LinkerTopic.AddToGroup;
                sendingButton.Content = "r";
            }
            bridge.SendRPCCommand(topic, sendingButton.Name);
            this.Hide();
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
