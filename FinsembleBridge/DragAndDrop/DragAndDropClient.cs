using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChartIQ.Finsemble
{
    public class DragAndDropClient
    {
        FinsembleBridge bridge;
        RouterClient routerClient;
        Dictionary<string, EventHandler<FinsembleEventArgs>> receivers = new Dictionary<string, EventHandler<FinsembleEventArgs>>();
        const string DRAG_START_CHANNEL = "DragAndDropClient.dragStart";
        const string DRAG_END_CHANNEL = "DragAndDropClient.dragEnd";
        Control scrim;
        enum ShareMethod { Drop, Spawn, Linker }

        public DragAndDropClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            routerClient = bridge.routerClient;
            
            routerClient.AddListener(DRAG_START_CHANNEL, (s, args) =>
            {
                if (scrim == null) return;
                var dataBeingShared = (args.response?["data"] as JArray).ToObject<List<string>>();
                dynamic a = scrim;
                if (canReceiveData(dataBeingShared))
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        scrim.AllowDrop = true;
                        try
                        {
                            a.Text = "*";
                        } catch
                        {
                            try
                            {
                                a.Content = "*";
                            } catch
                            {

                            }
                        }
                        scrim.Background = new SolidColorBrush(Color.FromArgb(237,32,104,195));
                        scrim.Visibility = Visibility.Visible;
                    });
                } else
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        scrim.AllowDrop = false;
                        try
                        {
                            a.Text = "D";
                        }
                        catch
                        {
                            try
                            {
                                a.Content = "D";
                            }
                            catch
                            {

                            }
                        }
                        scrim.Background = new SolidColorBrush(Color.FromArgb(237,150,10,5));
                        scrim.Visibility = Visibility.Visible;
                    });
                }
                
            });

            routerClient.AddListener(DRAG_END_CHANNEL, (s, args) =>
            {
                if (scrim == null) return;
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    scrim.Visibility = Visibility.Hidden;
                });
            });

        }

        private bool canReceiveData(List<string> dataTypes)
        {
            foreach (var dataType in dataTypes)
            {
                if(receivers.ContainsKey(dataType))
                {
                    return true;
                }
            }

            foreach(var receiver in receivers)
            {
                if(InternalHelper.IsRegex(receiver.Key))
                {
                    foreach(var dataType in dataTypes)
                    {
                        if(Regex.Match(dataType, receiver.Key).Success)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void setScrim(Control c)
        {
            scrim = c;
            scrim.PreviewDrop += Scrim_PreviewDrop;
            scrim.FontFamily = new FontFamily(new Uri("pack://application:,,,/FinsembleBridge;component/Resources/"), "./#finfont"); ///FinsembleBridge;component/Resources/#finfont
            scrim.FontSize = 100;
            scrim.Foreground = new SolidColorBrush(Colors.White);
        }

        private void HandleSharedData(JObject sharedData, ShareMethod shareMethod)
        {
            foreach(var receiver in receivers)
            {
                var data = new JObject { };
                foreach(var item in sharedData)
                {
                    if(receiver.Key == item.Key)
                    {
                        data[item.Key] = item.Value;
                    } else if (InternalHelper.IsRegex(receiver.Key) && Regex.Match(item.Key, receiver.Key).Success)
                    {
                        data[item.Key] = item.Value;
                    }
                }
                if(data.HasValues)
                {
                    receiver.Value?.Invoke(this, new FinsembleEventArgs(null, new JObject {
                        ["data"] = data,
                        ["shareMethod"] = shareMethod.ToString()
                    }));
                }
            }
        }

        private void Scrim_PreviewDrop(object sender, DragEventArgs e)
        {
            var jsonData = JObject.Parse(e.Data.GetData(DataFormats.StringFormat).ToString());
            if (jsonData["containsData"] == null)
            {
                routerClient.Query(jsonData["window"] + ".Share", jsonData["emitters"], new JObject { }, (s, args) => {
                    HandleSharedData(args.response?["data"] as JObject, ShareMethod.Drop);
                });
            } else
            {
                HandleSharedData(jsonData["data"] as JObject, ShareMethod.Drop);
            }
            e.Handled = true;
        }

        public void addReceivers(List<KeyValuePair<string, EventHandler<FinsembleEventArgs>>> receivers)
        {
            foreach(var receiver in receivers)
            {
                var type = receiver.Key;
                if (!this.receivers.ContainsKey(type))
                {
                    this.receivers.Add(type, receiver.Value);
                } else
                {
                    this.receivers[type] += receiver.Value;
                }
            }
        }

        public void removeReceivers(List<KeyValuePair<string, EventHandler<FinsembleEventArgs>>> receivers)
        {
            foreach (var receiver in receivers)
            {
                var type = receiver.Key;
                this.receivers[type] -= receiver.Value;
            }
        }

    }
}
