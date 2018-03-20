using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using EventHook;
using EventHook.Hooks;
using System.Diagnostics;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// This handles window movements, resizes and docking group membership
    /// </summary>
    internal class Docking
    {
        FinsembleBridge bridge;
        RouterClient routerClient;
        string dockingChannel;
        Window dockingWindow;
        string dockingWindowName;
        bool moving = false;
        bool resizing = false;
        double resizeScale;
        int resizeHandle;
        Point startPosition;

        private Point WindowLocation;
        private Point WindowBottomRight;

        private Point WindowResizeEndLocation;
        private Point WindowResizeEndBottomRight;

        const int WM_SIZING = 0x0214;
        const int WM_MOVING = 0x0216;

        const int WMSZ_BOTTOM = 6;
        const int WMSZ_BOTTOMLEFT = 7;
        const int WMSZ_BOTTOMRIGHT = 8;
        const int WMSZ_LEFT = 1;
        const int WMSZ_RIGHT = 2;
        const int WMSZ_TOP = 3;
        const int WMSZ_TOPLEFT = 4;
        const int WMSZ_TOPRIGHT = 5;

        [StructLayout(LayoutKind.Sequential)]
        private struct WIN32Rectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private DateTime lastMoveSent = DateTime.Now;
        private DateTime lastResizeSent = DateTime.Now;
        private DateTime lastStateChanged = DateTime.Now;

        public EventHandler<dynamic> DockingGroupUpdateHandler;

        private bool sendCloseToFinsemble = true;

        double dpiX, dpiY;

        internal Docking(FinsembleBridge _bridge, string channel)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                this.bridge = _bridge;
                routerClient = bridge.routerClient;
                this.dockingChannel = channel;
                this.dockingWindow = bridge.window;
                this.dockingWindowName = bridge.windowName;
                dockingWindow.Loaded += Window_Loaded;
                dockingWindow.Closing += Window_Closing;
                dockingWindow.Activated += Window_Activated;
                dockingWindow.StateChanged += DockingWindow_StateChanged;
                bridge.runtime.InterApplicationBus.subscribe("*", dockingChannel, Got_Docking_Message);
                MouseWatcher.OnMouseInput += MouseWatcher_OnMouseInput;
            });
        }

        private void DockingWindow_StateChanged(object sender, EventArgs e)
        {
            // prevent strange infinite loop from window somehow activating while minimize is called from docking
            TimeSpan t = DateTime.Now - lastStateChanged;
            if (t.TotalMilliseconds < 50) return;
            lastStateChanged = DateTime.Now;
            var w = (Window)sender;
            if (w.WindowState == WindowState.Normal)
            {
                Restore();
            }
            else if (w.WindowState == WindowState.Minimized)
            {
                Minimize();
            }
            else if (w.WindowState == WindowState.Maximized)
            {
                Maxmimize();
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            routerClient.Transmit(dockingWindowName + ".focused", new JObject { });
        }

        private void Got_Docking_Message(string sourceUuid, string topic, object message)
        {
            var joMessage = message as JObject;
            var action = joMessage.GetValue("action").ToString();

            switch (action)
            {
                case "setBounds":
                    var jsonMessage = joMessage.GetValue("bounds") as JObject;
                    var top = jsonMessage.GetValue("top").ToString();
                    var left = jsonMessage.GetValue("left").ToString();
                    var height = jsonMessage.GetValue("height").ToString();
                    var width = jsonMessage.GetValue("width").ToString();

                    if (string.IsNullOrEmpty(top)
                    || string.IsNullOrEmpty(left)
                    || string.IsNullOrEmpty(width)
                    || string.IsNullOrEmpty(height))
                    {
                        return;
                    }

                    /*Debug.Write(top + " ");
                    Debug.Write(left + " ");
                    Debug.Write(width + " ");
                    Debug.Write(height + " ");
                    Debug.WriteLine("");*/

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (!string.IsNullOrEmpty(top)) dockingWindow.Top = Double.Parse(top);

                        if (!string.IsNullOrEmpty(left)) dockingWindow.Left = Double.Parse(left);


                        if (!string.IsNullOrEmpty(height))
                        {
                            double h = Math.Abs(Double.Parse(height));
                            if (h > 0) dockingWindow.Height = h;
                        }


                        if (!string.IsNullOrEmpty(width))
                        {
                            double w = Math.Abs(Double.Parse(width));
                            if (w > 0) dockingWindow.Width = w;
                        }

                        WindowLocation = new Point(dockingWindow.Left, dockingWindow.Top);
                        WindowBottomRight = new Point(dockingWindow.Left + dockingWindow.Width, dockingWindow.Top + dockingWindow.Height);
                    });
                    break;
                case "bringToFront":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        dockingWindow.Topmost = true;
                        dockingWindow.Topmost = false;

                    });
                    break;
                case "setOpacity":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        dockingWindow.Opacity = Double.Parse(joMessage.GetValue("opacity").ToString());
                    });
                    break;
                case "hide":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        dockingWindow.Hide();
                    });
                    break;
                case "show":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        dockingWindow.Show();
                    });
                    break;
                /*case "groupUpdate":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        jsonMessage = joMessage.GetValue("groupData") as JObject;
                        dynamic groupData = new ExpandoObject();
                        groupData.dockingGroup = jsonMessage.GetValue("dockingGroup").ToString();
                        groupData.snappingGroup = jsonMessage.GetValue("snappingGroup").ToString();
                        DockingGroupUpdateHandler?.Invoke(this, groupData);
                    });
                    break;*/
                case "minimize":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (dockingWindow.WindowState != WindowState.Minimized) dockingWindow.WindowState = WindowState.Minimized;
                    });
                    break;
                case "restore":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (dockingWindow.WindowState != WindowState.Normal) dockingWindow.WindowState = WindowState.Normal;
                    });
                    break;
                case "maximize":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (dockingWindow.WindowState != WindowState.Maximized) dockingWindow.WindowState = WindowState.Maximized;
                    });
                    break;
                case "close":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        sendCloseToFinsemble = false;
                        dockingWindow.Close();
                    });
                    break;
            }
        }

        private void Minimize()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                //dockingWindow.WindowState = WindowState.Minimized;
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "minimize";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        private void Maxmimize()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                //dockingWindow.WindowState = WindowState.Maximized;
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "maximize";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        private void Restore()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                //dockingWindow.WindowState = WindowState.Normal;
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "restore";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        private void Hide()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dockingWindow.Hide();
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "hide";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        private void Show()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dockingWindow.Show();
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "show";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        private void BringToFront()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                //dockingWindow.BringIntoView();
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "bringToFront";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        internal void Close()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "close";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        /// <summary>
        /// Call from MouseDown event of control in Window Header that is responsible for moving the the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StartMove(dynamic sender, MouseButtonEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                sender.CaptureMouse();
                moving = true;
                startPosition = e.GetPosition(dockingWindow);
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.top = dockingWindow.Top;
                props.left = dockingWindow.Left;
                props.width = dockingWindow.Width;
                props.height = dockingWindow.Height;
                props.windowAction = "startMove";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        /// <summary>
        /// Call from MouseMove event of control in Window Header that is responsible for moving the the window
        /// </summary>
        public void Move(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (moving)
            {
                TimeSpan t = DateTime.Now - lastMoveSent;
                if (t.TotalMilliseconds < 20) return;
                Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                {
                    var currentPosition = e.GetPosition(dockingWindow);
                    double differenceX = currentPosition.X - startPosition.X;
                    double differenceY = currentPosition.Y - startPosition.Y;
                    dynamic props = new ExpandoObject();
                    props.windowName = dockingWindowName;
                    props.width = dockingWindow.Width;
                    props.height = dockingWindow.Height;
                    props.top = dockingWindow.Top + differenceY;
                    props.left = dockingWindow.Left + differenceX;
                    props.windowAction = "move";
                    bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                    lastMoveSent = DateTime.Now;
                });
            }

        }

        /// <summary>
        /// Call from MouseUp event of control in Window Header that is responsible for moving the the window
        /// </summary>
        public void EndMove(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "endMove";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                moving = false;
                Mouse.Capture(null);
            });
        }

        /// <summary>
        /// Call to dock snapped window
        /// </summary>
        public void FormGroup()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                routerClient.Transmit("DockingService.formGroup", new JObject
                {
                    ["windowName"] = bridge.windowName
                });
            });
        }

        /// <summary>
        /// Call to leave docking group
        /// </summary>
        public void LeaveGroup()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                routerClient.Query("DockingService.leaveGroup", new JObject
                {
                    ["name"] = bridge.windowName
                }, new JObject { }, (EventHandler<FinsembleEventArgs>)delegate (object s, FinsembleEventArgs args) { });
            });
        }

        public void GetWindowsInGroup(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.Query("DockingService.getWindowsInGroup", new JObject { ["groupName"] = parameters["groupName"] }, new JObject { }, (sender, args) =>
            {
                callback(sender, new FinsembleEventArgs(args.error, args.response?["data"]));
            });
        }

        private void Resize(Point TopCorner, Point BottomCorner)
        {
            TimeSpan t = DateTime.Now - lastResizeSent;
            if (t.TotalMilliseconds < 50) return;
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dynamic props = new JObject
                {
                    ["windowName"] = dockingWindowName,
                    ["width"] = BottomCorner.X - TopCorner.X,
                    ["height"] = BottomCorner.Y - TopCorner.Y,
                    ["top"] = TopCorner.Y,
                    ["left"] = TopCorner.X,
                    ["bottom"] = BottomCorner.Y,
                    ["right"] = BottomCorner.X,
                    /*["mousePosition"] = new JObject
                    {
                        ["x"] = MousePosition.X,
                        ["y"] = MousePosition.Y
                    },*/
                    ["windowAction"] = "resize"
                };
                bridge.SendRPCCommand("NativeWindow", props.ToString(), this.dockingChannel);
                lastResizeSent = DateTime.Now;
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                WindowInteropHelper helper = new WindowInteropHelper(dockingWindow);
                HwndSource.FromHwnd(helper.Handle).AddHook(HwndMessageHook);

                PresentationSource source = PresentationSource.FromVisual(dockingWindow);

                if (source != null)
                {
                    dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                    dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
                }

                WindowLocation = new Point(dockingWindow.Left, dockingWindow.Top);
                WindowBottomRight = new Point(dockingWindow.Left + dockingWindow.Width, dockingWindow.Top + dockingWindow.Height);

                var props = new JObject
                {
                    ["windowName"] = dockingWindowName,
                    ["top"] = dockingWindow.Top,
                    ["left"] = dockingWindow.Left,
                    ["width"] = dockingWindow.Width,
                    ["height"] = dockingWindow.Height,
                    ["windowAction"] = "open"
                };

                bridge.SendRPCCommand("NativeWindow", props.ToString(), dockingChannel);

            });

            routerClient.Subscribe("Finsemble.WorkspaceService.groupUpdate", (EventHandler<FinsembleEventArgs>)delegate (object s, FinsembleEventArgs args)
            {
                var groupData = args.response?["data"]?["groupData"] as JObject;
                if (groupData == null) return;
                dynamic thisWindowGroups = new ExpandoObject();
                thisWindowGroups.dockingGroup = "";
                thisWindowGroups.snappingGroup = "";
                foreach (var item in groupData)
                {
                    var windowsInGroup = item.Value["windowNames"] as JArray;
                    if (windowsInGroup.Where(window => (string)window == bridge.windowName).Count() > 0)
                    {
                        if ((bool)item.Value["isMovable"])
                        {
                            thisWindowGroups.dockingGroup = item.Key;
                        }
                        else
                        {
                            thisWindowGroups.snappingGroup = item.Key;
                        }
                    }
                }
                DockingGroupUpdateHandler?.Invoke(this, thisWindowGroups);
            });

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sendCloseToFinsemble)
            {
                this.Close();
            }
        }


        //https://stackoverflow.com/questions/12376141/intercept-a-move-event-in-a-wpf-window-before-the-move-happens-on-the-screen
        private IntPtr HwndMessageHook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool bHandled)
        {
            switch (msg)
            {
                case WM_SIZING:
                    if (!resizing)
                    {
                        TimeSpan t = DateTime.Now - lastResizeSent;
                        if (t.TotalMilliseconds < 250) return IntPtr.Zero; //stop resizing events from firing after resizeended - may or may not be a problem now
                        MouseWatcher.Start(); //start watching the mouse for movements and mouseup. Once watching starts, somehow resize ends so using the watcher also to resize
                    }
                    else //ignore if we are already resizing
                    {
                        bHandled = true;
                        return IntPtr.Zero;
                    }
                    resizing = true;

                    int sizeType = (int)wParam;
                    resizeHandle = sizeType;
                    double scale = 1;

                    WIN32Rectangle rectangle = (WIN32Rectangle)Marshal.PtrToStructure(lParam, typeof(WIN32Rectangle));

                    switch (sizeType)
                    {
                        case WMSZ_BOTTOM:
                        case WMSZ_BOTTOMLEFT:
                        case WMSZ_LEFT:
                        case WMSZ_BOTTOMRIGHT:
                            //get scale from top
                            scale = rectangle.Top / WindowLocation.Y;
                            break;

                        case WMSZ_RIGHT:
                        case WMSZ_TOP:
                        case WMSZ_TOPRIGHT:
                            //get dpi from left
                            scale = rectangle.Left / WindowLocation.X;
                            break;

                        case WMSZ_TOPLEFT:
                            //get dpi from right
                            scale = rectangle.Right / WindowBottomRight.X;
                            break;

                    }
                    bHandled = true;
                    resizeScale = scale;


                    break;

            }
            return IntPtr.Zero;
        }

        private void MouseWatcher_OnMouseInput(object sender, EventHook.MouseEventArgs e)
        {
            //var mousePosition = new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
            
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                /*if (Mouse.Captured == null) Mouse.Capture(dockingWindow);
                var mousePosition = Mouse.GetPosition(dockingWindow);
                //var mousePosition = dockingWindow.PointToScreen(RelativemousePosition);
                mousePosition.X = mousePosition.X + dockingWindow.Left;
                mousePosition.Y = mousePosition.Y + dockingWindow.Top;
                //Mouse.Capture(null);
                

                //var y = e.Point.y / resizeScale;
                //var x = e.Point.x / resizeScale;

                var y = mousePosition.Y;
                var x = mousePosition.X;*/

                if (e.Message == MouseMessages.WM_LBUTTONUP)
                {
                    resizing = false;
                    Resize(WindowResizeEndLocation, WindowResizeEndBottomRight);
                    dynamic props = new ExpandoObject();
                    props.windowName = dockingWindowName;
                    props.windowAction = "endMove";
                    bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                    lastResizeSent = DateTime.Now;
                    MouseWatcher.Stop();
                    Mouse.Capture(null);
                }
                else if (resizing && e.Message == MouseMessages.WM_MOUSEMOVE)
                {
                    WindowResizeEndLocation = new Point(dockingWindow.Left, dockingWindow.Top);
                    WindowResizeEndBottomRight = new Point(dockingWindow.Left + dockingWindow.Width, dockingWindow.Top + dockingWindow.Height);

                    /*WindowResizeEndLocation = new Point(WindowLocation.X, WindowLocation.Y);
                    WindowResizeEndBottomRight = new Point(WindowBottomRight.X, WindowBottomRight.Y);


                    switch (resizeHandle)
                    {
                        case WMSZ_BOTTOM:

                            WindowResizeEndBottomRight.Y = y;
                            break;
                        case WMSZ_BOTTOMLEFT:
                            WindowResizeEndBottomRight.Y = y;
                            WindowResizeEndLocation.X = x;
                            break;
                        case WMSZ_LEFT:
                            WindowResizeEndLocation.X = x;
                            break;
                        case WMSZ_BOTTOMRIGHT:
                            WindowResizeEndBottomRight.Y = y;
                            WindowResizeEndBottomRight.X = x;
                            break;
                        case WMSZ_RIGHT:
                            WindowResizeEndBottomRight.X = x;
                            break;
                        case WMSZ_TOP:
                            WindowResizeEndLocation.Y = y;
                            break;
                        case WMSZ_TOPRIGHT:
                            WindowResizeEndLocation.Y = y;
                            WindowResizeEndBottomRight.X = x;
                            break;
                        case WMSZ_TOPLEFT:
                            WindowResizeEndLocation.Y = y;
                            WindowResizeEndLocation.X = x;
                            break;
                    }*/

                    Resize(WindowResizeEndLocation, WindowResizeEndBottomRight);

                    /*Debug.WriteLine(WindowResizeEndLocation);
                    Debug.WriteLine(WindowResizeEndBottomRight);
                    Debug.WriteLine(mousePosition);*/

                    Debug.WriteLine(dockingWindow.Top);
                    Debug.WriteLine(dockingWindow.Left);
                    Debug.WriteLine(dockingWindow.ActualHeight);
                    Debug.WriteLine(dockingWindow.ActualWidth);

                }

            });

            //var y = mousePosition.Y;
            //var x = mousePosition.X;

            
        }

    }
}
