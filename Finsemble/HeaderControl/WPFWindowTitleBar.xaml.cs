using Newtonsoft.Json.Linq;
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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Newtonsoft.Json;
using System.Security;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// Interaction logic for FinsembleWPFHeader.xaml
    /// </summary>
    public partial class WPFWindowTitleBar : UserControl
    {
        public Finsemble bridge;
        private SortedDictionary<string, Button> LinkerGroups = new SortedDictionary<string, Button>();
        private string dockingGroup, snappingGroup;
        private bool dragging = false;
        private Brush activeBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C4C58"));
        private Brush inactiveBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#303D47"));
        private bool showLinker = true;
        double buttonHeight = 32;
        double buttonWidth = 32;

        private Brush dockingButtonDockedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF039BFF"));

        public WPFWindowTitleBar()
        {
            InitializeComponent();
            ParentContainer.SizeChanged += FinsembleHeader_SizeChanged;
            Toolbar.SizeChanged += FinsembleHeader_SizeChanged;
        }

        private void Linker_StateChange(object sender2, FinsembleEventArgs args)
        {
            Application.Current.Dispatcher.Invoke(delegate //main thread
            {
                try
                {
                    var channels = args.response["channels"] as JArray;
                    var allChannels = args.response["allChannels"] as JArray;

                    // Hide all LinkerGroups
                    foreach (var item in LinkerGroups)
                    {
                        item.Value.Visibility = Visibility.Hidden;
                    }
                    if (LinkerButton.Visibility != Visibility.Visible) { return;}

                    // Loop through Channels
                    Double baseLeft = buttonWidth + 4;
                    if (!showLinker) baseLeft -= buttonWidth;
                    Double increment = 12;
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
                                groupRectangle.VerticalAlignment = VerticalAlignment.Center;
                                groupRectangle.Width = 7;
                                groupRectangle.Height = 20;
                                groupRectangle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)item["color"]));
                                Toolbar.Children.Add(groupRectangle);
                                groupRectangle.SetValue(Canvas.TopProperty, (Toolbar.ActualHeight - groupRectangle.Height) / 2);
                                groupRectangle.Name = groupName;
                                var style = this.Resources["LinkerPillStyle"];
                                groupRectangle.SetValue(StyleProperty, style);
                                LinkerGroups[groupName] = groupRectangle;
                                groupRectangle.Click += LinkerPill_Click;
                            }
                            LinkerGroups[groupName].SetValue(Canvas.LeftProperty, baseLeft);
                            baseLeft += increment;
                            LinkerGroups[groupName].Visibility = Visibility.Visible;
                        }
                    }
                    Window_Size_Changed();
                }
                catch
                {

                }
            });
        }


        private void FinsembleHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            buttonHeight = ParentContainer.ActualHeight;
            buttonWidth = ParentContainer.ActualHeight; // currently buttons are square and all same size
            Toolbar.Height = buttonHeight;

            Linker.Height = buttonHeight;
            DockingButton.Height = buttonHeight;
            Emitter.Height = buttonHeight;
            Maximize.Height = buttonHeight;
            Minimize.Height = buttonHeight;
            Close.Height = buttonHeight;
            Title.Height = buttonHeight;

            Linker.Width = buttonWidth;
            DockingButton.Width = buttonWidth;
            Emitter.Width = buttonWidth;
            Maximize.Width = buttonWidth;
            Minimize.Width = buttonWidth;
            Close.Width = buttonWidth;
            

            Window_Size_Changed();
        }

        public void SetTitle(string title)
        {
            Title.Content = title;
        }

        public void SetActiveBackground(Brush background)
        {
            activeBackground = background;
        }

        public void SetInactiveBackground(Brush background)
        {
            inactiveBackground = background;
        }

        /// <summary>
        /// Set Font For Title. Use null for Family, Style and Weight to not change. Use 0 for Size to not change.
        /// </summary>
        /// <param name="fontFamily"></param>
        /// <param name="fontSize"></param>
        /// <param name="fontStyle"></param>
        /// <param name="fontWeight"></param>
        public void SetTitleFont(FontFamily fontFamily, double fontSize, FontStyle fontStyle, FontWeight fontWeight)
        {
            if (fontFamily != null) Title.FontFamily = fontFamily;
            if (fontSize != 0) Title.FontSize = fontSize;
            if (fontStyle != null) Title.FontStyle = fontStyle;
            if (fontWeight != null) Title.FontWeight = fontWeight;
        }

        /// <summary>
        /// Set Font For Buttons. Use null for Family, Style and Weight to not change. Use 0 for Size to not change.
        /// </summary>
        /// <param name="fontFamily"></param>
        /// <param name="fontSize"></param>
        /// <param name="fontStyle"></param>
        /// <param name="fontWeight"></param>
        public void SetButtonFont(FontFamily fontFamily, double fontSize, FontStyle fontStyle, FontWeight fontWeight)
        {
            if (fontFamily != null) {
                Linker.FontFamily = fontFamily;
                DockingButton.FontFamily = fontFamily;
                Emitter.FontFamily = fontFamily;
                Maximize.FontFamily = fontFamily;
                Minimize.FontFamily = fontFamily;
                Close.FontFamily = fontFamily;
            }
            if (fontSize != 0)
            {
                Linker.FontSize = fontSize;
                DockingButton.FontSize = fontSize;
                Emitter.FontSize = fontSize;
                Maximize.FontSize = fontSize;
                Minimize.FontSize = fontSize;
                Close.FontSize = fontSize;
            }
            if (fontStyle != null)
            {
                Linker.FontStyle = fontStyle;
                DockingButton.FontStyle = fontStyle;
                Emitter.FontStyle = fontStyle;
                Maximize.FontStyle = fontStyle;
                Minimize.FontStyle = fontStyle;
                Close.FontStyle = fontStyle;
            }
            if (fontWeight != null)
            {
                Linker.FontWeight = fontWeight;
                DockingButton.FontWeight = fontWeight;
                Emitter.FontWeight = fontWeight;
                Maximize.FontWeight = fontWeight;
                Minimize.FontWeight = fontWeight;
                Close.FontWeight = fontWeight;
            }
        }

        public void SetButtonHoverBackground(SolidColorBrush color)
        {
            this.Resources["Button.MouseOver.Background"] = color;
            this.Resources["Button.Pressed.Background"] = color;
        }

        public void SetInactiveButtonHoverBackground(SolidColorBrush color)
        {
            this.Resources["InactiveButton.MouseOver.Background"] = color;
            this.Resources["InactiveButton.Pressed.Background"] = color;
        }

        public void SetCloseButtonHoverBackground(SolidColorBrush color)
        {
            this.Resources["CloseButton.MouseOver.Background"] = color;
            this.Resources["CloseButton.Pressed.Background"] = color;
        }

        public void SetInactiveCloseButtonHoverBackground(SolidColorBrush color)
        {
            this.Resources["InactiveCloseButton.MouseOver.Background"] = color;
            this.Resources["InactiveCloseButton.Pressed.Background"] = color;
        }

        public void SetDockingButtonDockedBackground(SolidColorBrush color)
        {
            dockingButtonDockedBackground = color;
        }

        public void SetTitleForeground(SolidColorBrush color)
        {
            Title.Foreground = color;
        }

        public void SetButtonForeground(SolidColorBrush color)
        {
            Linker.Foreground = color;
            DockingButton.Foreground = color;
            Emitter.Foreground = color;
            Maximize.Foreground = color;
            Minimize.Foreground = color;
            Close.Foreground = color;
        }

        public Button LinkerButton => Linker;
        public void SetBridge(Finsemble finsemble)
        {
            bridge = finsemble;
            bridge.docking.DockingGroupUpdateHandler += Docking_GroupUpdate;
            bridge.LinkerClient.OnStateChange(Linker_StateChange);
            if (bridge.componentConfig?["foreign"]?["components"]?["Window Manager"]?["showLinker"] != null) showLinker = (bool) bridge.componentConfig["foreign"]["components"]["Window Manager"]["showLinker"];
            if (!showLinker) Linker.Visibility = Visibility.Hidden;
            Application.Current.Dispatcher.Invoke(delegate //main thread
            {
                bridge.window.Activated += Window_Activated;
                bridge.window.Deactivated += Window_Deactivated;
            });
            bridge.DragAndDropClient.AddEmitterChangeListener((s, e) =>
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    if (e)
                    {
                        Emitter.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Emitter.Visibility = Visibility.Hidden;
                    }
                });
            });
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Toolbar.Background = inactiveBackground;
            Linker.Style = (Style)this.Resources["InactiveToolbarRegularButton"];
            DockingButton.Style = (Style)this.Resources["InactiveToolbarRegularButton"];
            Emitter.Style = (Style)this.Resources["InactiveToolbarRegularLabel"];
            Maximize.Style = (Style)this.Resources["InactiveToolbarRegularButton"];
            Minimize.Style = (Style)this.Resources["InactiveToolbarRegularButton"];
            Close.Style = (Style)this.Resources["InactiveToolbarCloseButton"];
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Toolbar.Background = activeBackground;
            Linker.Style = (Style)this.Resources["ToolbarRegularButton"];
            DockingButton.Style = (Style)this.Resources["ToolbarRegularButton"];
            Emitter.Style = (Style)this.Resources["ToolbarRegularLabel"];
            Maximize.Style = (Style)this.Resources["ToolbarRegularButton"];
            Minimize.Style = (Style)this.Resources["ToolbarRegularButton"];
            Close.Style = (Style)this.Resources["ToolbarCloseButton"];
        }

        private void Docking_GroupUpdate(object sender, dynamic groups)
        {
            Application.Current.Dispatcher.Invoke(delegate //main thread
            {
                this.dockingGroup = groups.dockingGroup;
                this.snappingGroup = groups.snappingGroup;
                Minimize.Visibility = Visibility.Visible;
                if (groups.dockingGroup != "")
                {
                    DockingButton.Content = "@";
                    DockingButton.ToolTip = "Detach Window";
                    DockingButton.Visibility = Visibility.Visible;
                    Minimize.SetValue(Canvas.RightProperty, buttonWidth * 3);
                    DockingButton.Background = dockingButtonDockedBackground;
                    if (!groups.topRight)
                    {
                        Minimize.Visibility = Visibility.Hidden;
                    }
                }
                else if (groups.snappingGroup != "")
                {
                    DockingButton.Content = ">";
                    DockingButton.ToolTip = "Attach Windows";
                    DockingButton.Visibility = Visibility.Visible;
                    Minimize.SetValue(Canvas.RightProperty, buttonWidth * 3);
                    DockingButton.Background = Brushes.Transparent;
                }
                else
                {
                    DockingButton.Visibility = Visibility.Hidden;
                    Minimize.SetValue(Canvas.RightProperty, buttonWidth * 2);
                }
                Window_Size_Changed();
            });
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(delegate //main thread
            {
                bridge.window.WindowState = WindowState.Minimized;
            });
        }

        private void Toolbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                return;
            }
            bridge.window.DragMove(); // this does the work
            //bridge.docking.StartMove(sender, e);
        }

        private void Toolbar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //bridge.docking.EndMove(sender, e);
        }

        private void Toolbar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //bridge.docking.Move(sender, e);
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            var senderButton = (System.Windows.Controls.Button)sender;
            if (bridge.window.WindowState == System.Windows.WindowState.Maximized)
            {
                Application.Current.Dispatcher.Invoke(delegate //main thread
                {
                    bridge.window.WindowState = WindowState.Normal;
                });
                senderButton.Content = "3";
            }
            else
            {
                Application.Current.Dispatcher.Invoke(delegate //main thread
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
            bridge.LinkerClient.ShowLinkerWindow(0, Linker.ActualHeight);
        }

        private List<string> getWindowList(string linkerChannel, bool includeAppSuites, bool includeDockedGroups)
        {
            var windowList = new List<string>();
            var taskCompletionList = new List<TaskCompletionSource<List<string>>>();
            var taskList = new List<Task<List<string>>>();

            if (!string.IsNullOrEmpty(linkerChannel))
            {
                var linkerChannelTaskCompletionSource = new TaskCompletionSource<List<string>>();
                var linkerChannelTask = linkerChannelTaskCompletionSource.Task;
                var channels = new JArray();
                channels.Add(linkerChannel);
                bridge.LinkerClient.GetLinkedComponents(new JObject { ["channels"] = channels }, (s, args) =>
                {
                    var linkedWindowList = (args.response as JArray).ToObject<List<string>>();
                    linkerChannelTaskCompletionSource.SetResult(linkedWindowList);
                });
                windowList.AddRange(linkerChannelTask.Result);
            }

            if (includeAppSuites)
            {
                var appSuitesTaskCompletionSource = new TaskCompletionSource<List<string>>();
                var appSuitesTask = appSuitesTaskCompletionSource.Task;
                int numberOfGroups = 0;
                var appSuitesList = new List<string>();
                bridge.launcherClient.GetGroupsForWindow((s, args) =>
                {
                    var groups = args.response as JObject;
                    numberOfGroups = groups.Properties().Count();
                    foreach (var g in groups)
                    {
                        bridge.launcherClient.GetWindowsInGroup(new JObject
                        {
                            ["groupName"] = g.Value
                        }, (s2, args2) =>
                        {
                            args.ToString();
                            numberOfGroups--;
                            if (numberOfGroups == 0)
                            {
                                appSuitesTaskCompletionSource.SetResult(appSuitesList);
                            }
                        });
                    }
                });
                windowList.AddRange(appSuitesTask.Result);

            }

            if (includeDockedGroups)
            {
                var dockingGroupTaskCompletionSource = new TaskCompletionSource<List<string>>();
                var dockingGroupChannelTask = dockingGroupTaskCompletionSource.Task;
                bridge.docking.GetWindowsInGroup(new JObject
                {
                    ["groupName"] = dockingGroup
                }, (s, args) =>
                {
                    if (args.response.HasValues)
                    {
                        var dockingGroupList = (args.response as JArray).ToObject<List<string>>();
                        dockingGroupTaskCompletionSource.SetResult(dockingGroupList);
                    }
                    else
                    {
                        dockingGroupTaskCompletionSource.SetResult(new List<string>());
                    }

                });
                windowList.AddRange(dockingGroupChannelTask.Result);
            }

            return windowList;

        }

        private void hyperFocus(string linkerChannel, bool includeAppSuites, bool includeDockingGroups)
        {
            var windowList = getWindowList(linkerChannel, includeAppSuites, includeDockingGroups);
            bridge.launcherClient.HyperFocus(new JObject
            {
                ["windowList"] = JArray.FromObject(windowList)
            }, (s, args) => { });
        }

        private void bringToFront(string linkerChannel, bool includeAppSuites, bool includeDockingGroups)
        {
            var windowList = getWindowList(linkerChannel, includeAppSuites, includeDockingGroups);
            bridge.launcherClient.BringWindowsToFront(new JObject
            {
                ["windowList"] = JArray.FromObject(windowList)
            }, (s, args) => { });
        }

        private void LinkerPill_Click(object sender, RoutedEventArgs e)
        {
            var sendingButton = (Button)sender;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                hyperFocus(sendingButton.Name, false, true);
            }
            else
            {
                bringToFront(sendingButton.Name, false, false);
            }
        }

        private void Window_Size_Changed()
        {
            int LinkerGroupCount = LinkerGroups.Where(g => g.Value.Visibility == Visibility.Visible).Count();
            double LeftWidths = buttonWidth + LinkerGroupCount * 12;
            double RightWidths = buttonWidth * 3;

            if (!showLinker)
            {
                LeftWidths -= buttonWidth;
            }

            if (Emitter.Visibility == Visibility.Visible)
            {
                Emitter.SetValue(Canvas.LeftProperty, LeftWidths);
                LeftWidths += buttonWidth;
            }
            if (DockingButton.IsVisible) RightWidths = buttonWidth * 4;
            Title.SetValue(Canvas.LeftProperty, LeftWidths);
            Close.SetValue(Canvas.RightProperty, 0.0);
            Maximize.SetValue(Canvas.RightProperty, buttonWidth);
            if (DockingButton.Visibility == Visibility.Visible)
            {
                Minimize.SetValue(Canvas.RightProperty, buttonWidth * 3);
                DockingButton.SetValue(Canvas.RightProperty, buttonWidth * 2);
            } else
            {
                Minimize.SetValue(Canvas.RightProperty, buttonWidth * 2);
            }

            var titleWidth = Toolbar.ActualWidth - LeftWidths - RightWidths;
            if (titleWidth < 0) titleWidth = 0;
            Title.Width = titleWidth;
        }

        private void Docking_Click(object sender, RoutedEventArgs e)
        {
            if (DockingButton.Content == "@")
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    hyperFocus(null, false, true);
                }
                else
                {
                    bridge.docking.LeaveGroup();
                }
            }
            else
            {
                bridge.docking.FormGroup();
            }
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
            bridge.DragAndDropClient.DragStartWithData(sender);
        }

        private void AppSuites_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
