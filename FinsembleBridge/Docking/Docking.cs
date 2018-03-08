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

namespace ChartIQ.Finsemble
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WIN32Rectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public class Docking
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

        private DateTime lastMoveSent = DateTime.Now;
        private DateTime lastResizeSent = DateTime.Now;

        public EventHandler<dynamic> DockingGroupUpdateHandler;

        private bool sendCloseToFinsemble = true;

        double dpiX, dpiY;

        public Docking(FinsembleBridge _bridge, string channel)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                this.bridge = _bridge;
                routerClient = bridge.routerClient;
                this.dockingChannel = channel;
                this.dockingWindow = bridge.window;
                dockingWindow.Loaded += Window_Loaded;
                dockingWindow.Closing += Window_Closing;
                this.dockingWindowName = bridge.windowName;
                bridge.runtime.InterApplicationBus.subscribe("*", dockingChannel, Got_Docking_Message);
                MouseWatcher.OnMouseInput += MouseWatcher_OnMouseInput;
            });
        }

        private void Got_Docking_Message(string sourceUuid, string topic, object message)
        {
            var joMessage = message as JObject;
            var action = joMessage.GetValue("action").ToString();

            switch (action)
            {
                case "setBounds":
                    var jsonMessage = joMessage.GetValue("bounds") as JObject;
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        var top = jsonMessage.GetValue("top").ToString();
                        if (!string.IsNullOrEmpty(top)) dockingWindow.Top = Double.Parse(top);
                        var left = jsonMessage.GetValue("left").ToString();
                        if (!string.IsNullOrEmpty(left)) dockingWindow.Left = Double.Parse(left);
                        var height = jsonMessage.GetValue("height").ToString();
                        if (!string.IsNullOrEmpty(height)) dockingWindow.Height = Double.Parse(height);
                        var width = jsonMessage.GetValue("width").ToString();
                        if (!string.IsNullOrEmpty(width)) dockingWindow.Width = Double.Parse(width);
                        WindowLocation = new Point(dockingWindow.Left, dockingWindow.Top);
                        WindowBottomRight = new Point(dockingWindow.Left + dockingWindow.Width, dockingWindow.Top + dockingWindow.Height);
                    });
                    break;
                case "bringToFront":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        dockingWindow.BringIntoView();
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
                        dockingWindow.WindowState = WindowState.Minimized;
                    });
                    break;
                case "restore":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        dockingWindow.WindowState = WindowState.Normal;
                    });
                    break;
                case "maximize":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        dockingWindow.WindowState = WindowState.Maximized;
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

        public void Minimize()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dockingWindow.WindowState = WindowState.Minimized;
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "minimize";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        public void Maxmimize()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dockingWindow.WindowState = WindowState.Maximized;
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "maximize";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        public void Restore()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dockingWindow.WindowState = WindowState.Normal;
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "restore";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        public void Hide()
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

        public void Show()
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

        public void BringToFront()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dockingWindow.BringIntoView();
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "bringToFront";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        public void Close()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "close";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

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

        public void FormGroup(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "formGroup";
                //bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                routerClient.transmit("DockingService.formGroup", new JObject
                {
                    ["windowName"] = bridge.windowName
                });
            });
        }

        public void LeaveGroup(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "leaveGroup";
                //bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                routerClient.query("DockingService.leaveGroup", new JObject
                {
                    ["name"] = bridge.windowName
                }, new JObject { }, (EventHandler<FinsembleEventArgs>)delegate (object s, FinsembleEventArgs args) { });
            });
        }

        public void Resize(Point TopCorner, Point BottomCorner)
        {
            TimeSpan t = DateTime.Now - lastResizeSent;
            if (t.TotalMilliseconds < 50) return;
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.width = BottomCorner.X - TopCorner.X;
                props.height = BottomCorner.Y - TopCorner.Y;
                props.top = TopCorner.Y;
                props.left = TopCorner.X;
                props.windowAction = "resize";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                lastResizeSent = DateTime.Now;
            });
        }

        public void Window_Loaded(object sender, RoutedEventArgs e)
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

                var props = new JObject {
                    ["windowName"] = dockingWindowName,
                    ["top"] = dockingWindow.Top,
                    ["left"] = dockingWindow.Left,
                    ["width"] = dockingWindow.Width,
                    ["height"] = dockingWindow.Height,
                    ["windowAction"] = "open"
                };
                
                bridge.SendRPCCommand("NativeWindow", props.ToString(), dockingChannel);

            });

            routerClient.subscribe("Finsemble.WorkspaceService.groupUpdate", (EventHandler<FinsembleEventArgs>)delegate (object s, FinsembleEventArgs args)
            {
                var groupData = args.response?["data"]?["groupData"] as JObject;
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
                    } else //ignore if we are already resizing
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
            if (e.Message == MouseMessages.WM_LBUTTONUP)
            {
                resizing = false;
                Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                {
                    MouseWatcher.Stop();
                    Resize(WindowResizeEndLocation, WindowResizeEndBottomRight);
                    dynamic props = new ExpandoObject();
                    props.windowName = dockingWindowName;
                    props.windowAction = "endMove";
                    bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                    lastResizeSent = DateTime.Now;
                });
            }
            else if (resizing && e.Message == MouseMessages.WM_MOUSEMOVE)
            {
                WindowResizeEndLocation = new Point(WindowLocation.X, WindowLocation.Y);
                WindowResizeEndBottomRight = new Point(WindowBottomRight.X, WindowBottomRight.Y);

                switch (resizeHandle)
                {
                    case WMSZ_BOTTOM:
                        WindowResizeEndBottomRight.Y = e.Point.y / resizeScale;
                        break;
                    case WMSZ_BOTTOMLEFT:
                        WindowResizeEndBottomRight.Y = e.Point.y / resizeScale;
                        WindowResizeEndLocation.X = e.Point.x / resizeScale;
                        break;
                    case WMSZ_LEFT:
                        WindowResizeEndLocation.X = e.Point.x / resizeScale;
                        break;
                    case WMSZ_BOTTOMRIGHT:
                        WindowResizeEndBottomRight.Y = e.Point.y / resizeScale;
                        WindowResizeEndBottomRight.X = e.Point.x / resizeScale;
                        break;
                    case WMSZ_RIGHT:
                        WindowResizeEndBottomRight.X = e.Point.x / resizeScale;
                        break;
                    case WMSZ_TOP:
                        WindowResizeEndLocation.Y = e.Point.y / resizeScale;
                        break;
                    case WMSZ_TOPRIGHT:
                        WindowResizeEndLocation.Y = e.Point.y / resizeScale;
                        WindowResizeEndBottomRight.X = e.Point.x / resizeScale;
                        break;
                    case WMSZ_TOPLEFT:
                        WindowResizeEndLocation.Y = e.Point.y / resizeScale;
                        WindowResizeEndLocation.X = e.Point.x / resizeScale;
                        break;
                }
                Resize(WindowResizeEndLocation, WindowResizeEndBottomRight);
            }
        }

    }
}
