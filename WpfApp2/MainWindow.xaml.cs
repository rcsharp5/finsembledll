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
using ChartIQ.Finsemble;
using Newtonsoft.Json.Linq;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FinsembleBridge finsemble;
        private string windowName;
        private string componentType = "Unknown";
        private string top, left, height, width, uuid;

        private void SpawnChart_Click(object sender, RoutedEventArgs e)
        {
            finsemble.SendCommand("LauncherClient.spawn", new List<JToken> {
                "Advanced Chart",
                new JObject { }
            }, (s, a) => { });
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            finsemble.SendCommand("LinkerClient.publish", new List<JToken>
            {
                new JObject {
                    ["dataType"] = "symbol",
                    ["data"] = DataToSend.Text
                }
            }, (s, args) => { });
        }

        public MainWindow(string FinsembleWindowName, string componentType, string top, string left, string height, string width, string uuid)
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
            this.uuid = uuid;

            finsemble = new FinsembleBridge(new System.Version("8.56.28.34"), windowName, componentType, this, uuid);
            finsemble.Connect();
            finsemble.Connected += Bridge_Connected;
        }

        private void Bridge_Connected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                // Initialize this Window and show it
                InitializeComponent();
                FinsembleHeader.setBridge(finsemble);
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

                finsemble.dragAndDropClient.SetScrim(Scrim);

                finsemble.dragAndDropClient.AddReceivers(new List<KeyValuePair<string, EventHandler<FinsembleEventArgs>>>()
                {
                    new KeyValuePair<string, EventHandler<FinsembleEventArgs>>("symbol", (s, args) =>
                    {
                        var data = args.response["data"]?["symbol"]?["symbol"];
                        if(data != null)
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                            {
                                DroppedData.Content = data.ToString();
                                DataToSend.Text = data.ToString();
                            });
                        };
                    })
                });

                finsemble.dragAndDropClient.SetEmitters(new List<KeyValuePair<string, DragAndDropClient.emitter>>()
                {
                    new KeyValuePair<string, DragAndDropClient.emitter>("symbol", () =>
                    {
                        return new JObject
                        {
                            ["symbol"] = DataToSend.Text,
                            ["description"] = "Symbol " + DataToSend.Text
                        };
                    })
                });

                this.Show();

            });

            finsemble.SendCommand("LinkerClient.subscribe", new List<JToken>
            {
                "symbol"
            }, (s, args) =>
            {
                Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                {
                    DataToSend.Text = args.response["data"].ToString();
                    DroppedData.Content = args.response["data"].ToString();
                });
            });
        }
    }
}
