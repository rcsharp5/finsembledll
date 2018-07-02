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
/*using EventHook;
using EventHook.Hooks;*/
using System.Diagnostics;


namespace ChartIQ.Finsemble
{
    /// <summary>
    /// This handles window movements, resizes and docking group membership
    /// </summary>
    internal class Docking
    {
        Finsemble bridge;
        RouterClient routerClient;
        string dockingChannel;
        Window dockingWindow;
        string dockingWindowName;
        bool moving = false;
        bool resizing = false;
        /*double resizeScale;*/
        int resizeHandle;
        Point startPosition;

        private Point WindowLocation;
        private Point WindowBottomRight;

        private Point WindowResizeEndLocation;
        private Point WindowResizeEndBottomRight;

        const int WM_SIZING = 0x0214;
        const int WM_MOVING = 0x0216;
        const int WM_MOUSEMOVE = 0x200;
        const int WM_LBUTTONDOWN = 0x201;
        const int WM_LBUTTONUP = 0x202;
        const int WM_EXITSIZEMOVE = 0x232;

        const int WMSZ_BOTTOM = 6;
        const int WMSZ_BOTTOMLEFT = 7;
        const int WMSZ_BOTTOMRIGHT = 8;
        const int WMSZ_LEFT = 1;
        const int WMSZ_RIGHT = 2;
        const int WMSZ_TOP = 3;
        const int WMSZ_TOPLEFT = 4;
        const int WMSZ_TOPRIGHT = 5;
        

        private WIN32Rectangle windowRect;
        private WIN32Rectangle newWindowRect;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out WIN32Rectangle lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern bool EnumDisplaySettingsEx([MarshalAs(UnmanagedType.LPStr)]string lpszDeviceName, int iModeNum, out DEVMODE lpDevMode, uint dwFlags);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalSize(IntPtr handle);     
        
        //https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/NativeMethods.cs,478d6b005e9903a6
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        //https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/ScreenOrientation.cs,29768eea794e249c
        public enum ScreenOrientation
        {
            /// <include file='doc\ScreenOrientation.uex' path='docs/doc[@for="Day.Angle0"]/*' />
            /// <devdoc>
            ///    <para>
            ///       The screen is oriented at 0 degrees
            ///    </para>
            /// </devdoc>
            Angle0 = 0,

            /// <include file='doc\ScreenOrientation.uex' path='docs/doc[@for="Day.Angle90"]/*' />
            /// <devdoc>
            ///    <para>
            ///       The screen is oriented at 90 degrees
            ///    </para>
            /// </devdoc>
            Angle90 = 1,

            /// <include file='doc\ScreenOrientation.uex' path='docs/doc[@for="Day.Angle180"]/*' />
            /// <devdoc>
            ///    <para>
            ///       The screen is oriented at 180 degrees.
            ///    </para>
            /// </devdoc>
            Angle180 = 2,

            /// <include file='doc\ScreenOrientation.uex' path='docs/doc[@for="Day.Angle270"]/*' />
            /// <devdoc>
            ///    <para>
            ///       The screen is oriented at 270 degrees.
            ///    </para>
            /// </devdoc>
            Angle270 = 3,
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern int EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

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
        public bool hidden = false;

        private bool sendCloseToFinsemble = true;

        double dpiX, dpiY;

        internal Docking(Finsemble _bridge, string channel)
        {
            Application.Current.Dispatcher.Invoke(delegate //main thread
            {
                this.bridge = _bridge;
                routerClient = bridge.RouterClient;
                this.dockingChannel = channel;
                this.dockingWindow = bridge.window;
                this.dockingWindowName = bridge.windowName;
                dockingWindow.Loaded += Window_Loaded;
                dockingWindow.Closing += Window_Closing;
                dockingWindow.Activated += Window_Activated;
                dockingWindow.StateChanged += DockingWindow_StateChanged;
                bridge.runtime.InterApplicationBus.subscribe("*", dockingChannel, Got_Docking_Message); // Finsemble 2.3
                routerClient.AddListener("FinsembleNativeActions." + bridge.windowName, Got_Docking_Message_Over_Router); // Finsemble 2.5+
                //dockingWindow.GotMouseCapture += DockingWindow_GotMouseCapture;
                //dockingWindow.LostMouseCapture += DockingWindow_LostMouseCapture;
                //MouseWatcher.OnMouseInput += MouseWatcher_OnMouseInput;
                routerClient.AddListener("LauncherService.shutdownRequest", (s, e) =>
                {
                    sendCloseToFinsemble = false;
                });
            });

        }

        private void Got_Docking_Message_Over_Router(object sender, FinsembleEventArgs e)
        {
            Got_Docking_Message(null, null, e.response["data"]);
        }

        private void DockingWindow_LostMouseCapture(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("Lost Mouse Capture");
        }

        private void DockingWindow_GotMouseCapture(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("Got Mouse Capture");
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

                    var dtop = Double.Parse(top);
                    var dleft = Double.Parse(left);
                    var dheight = Double.Parse(height);
                    var dwidth = Double.Parse(width);
                    if (dheight < 32) dheight = 32;
                    if (dwidth < 1) dwidth = 1;

                    /*Debug.Write(top + " ");
                    Debug.Write(left + " ");
                    Debug.Write(width + " ");
                    Debug.Write(height + " ");
                    Debug.WriteLine("");*/

                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        var topLeftChanged = false;
                        if (dockingWindow.Top != dtop) { dockingWindow.Top = dtop; topLeftChanged = true; }
                        if (dockingWindow.Left != dleft) { dockingWindow.Left = dleft; topLeftChanged = true; }
                        if (dockingWindow.Height != dheight) dockingWindow.Height = dheight;
                        if (dockingWindow.Width != dwidth) dockingWindow.Width = dwidth;

                        if (topLeftChanged) WindowLocation = new Point(dockingWindow.Left, dockingWindow.Top);
                        WindowBottomRight = new Point(dockingWindow.Left + dockingWindow.Width, dockingWindow.Top + dockingWindow.Height);
                    });

                    //If we have unscaled values then set the windowRect
                    /*var unscaledTop = jsonMessage.GetValue("unscaledTop").ToString();
                    var unscaledLeft = jsonMessage.GetValue("unscaledLeft").ToString();
                    var unscaledBottom = jsonMessage.GetValue("unscaledBottom").ToString();
                    var unscaledRight = jsonMessage.GetValue("unscaledRight").ToString();

                    if (string.IsNullOrEmpty(unscaledTop)
                    || string.IsNullOrEmpty(unscaledLeft)
                    || string.IsNullOrEmpty(unscaledBottom)
                    || string.IsNullOrEmpty(unscaledRight))
                    {
                        return;
                    }

                    windowRect.Top = int.Parse(unscaledTop);
                    windowRect.Left = int.Parse(unscaledLeft);
                    windowRect.Bottom = int.Parse(unscaledBottom);
                    windowRect.Right = int.Parse(unscaledRight);*/

                    break;
                case "bringToFront":
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        dockingWindow.Topmost = true;
                        dockingWindow.Topmost = false;

                    });
                    break;
                case "setOpacity":
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        if (!resizing)
                        {
                            dockingWindow.Opacity = Double.Parse(joMessage.GetValue("opacity").ToString());
                        }
                    });
                    break;
                case "hide":
                    hidden = true;
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        dockingWindow.Opacity = 0.0;
                        //dockingWindow.Hide();
                    });
                    break;
                case "show":
                    hidden = false;
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        dockingWindow.Opacity = 1.0;
                        //dockingWindow.Show();
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
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        if (dockingWindow.WindowState != WindowState.Minimized) dockingWindow.WindowState = WindowState.Minimized;
                    });
                    break;
                case "restore":
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        if (dockingWindow.WindowState != WindowState.Normal) dockingWindow.WindowState = WindowState.Normal;
                    });
                    break;
                case "maximize":
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        if (dockingWindow.WindowState != WindowState.Maximized) dockingWindow.WindowState = WindowState.Maximized;
                    });
                    break;
                case "close":
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        sendCloseToFinsemble = false;
                        dockingWindow.Close();
                    });
                    break;
            }
        }

        private void Minimize()
        {
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            if (resizing) return;
            Debug.WriteLine("start move");
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
                props.scaled = true;
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
                Debug.WriteLine("move");
                TimeSpan t = DateTime.Now - lastMoveSent;
                if (t.TotalMilliseconds < 20) return;
                Application.Current.Dispatcher.Invoke(delegate //main thread
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
                    props.scaled = true;
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
            Debug.WriteLine("end move");
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            Application.Current.Dispatcher.Invoke(delegate //main thread
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
            if (t.TotalMilliseconds < 35) return;
            Application.Current.Dispatcher.Invoke(delegate //main thread
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

        private void Resize(WIN32Rectangle newWindowRect, bool force = false)
        {
            if (!force)
            {
                TimeSpan t = DateTime.Now - lastResizeSent;
                if (t.TotalMilliseconds < 35) return;
            }
            var props = new JObject
            {
                ["windowName"] = dockingWindowName,
                ["width"] = newWindowRect.Right - newWindowRect.Left,
                ["height"] = newWindowRect.Bottom - newWindowRect.Top,
                ["top"] = newWindowRect.Top,
                ["left"] = newWindowRect.Left,
                ["bottom"] = newWindowRect.Bottom,
                ["right"] = newWindowRect.Right,
                ["scaled"] = false,
                ["windowAction"] = "resize"
            };
            bridge.SendRPCCommand("NativeWindow", props.ToString(), this.dockingChannel);
            lastResizeSent = DateTime.Now;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(delegate //main thread
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

            routerClient.Subscribe("Finsemble.WorkspaceService.groupUpdate", delegate (object s, FinsembleEventArgs args)
            {
                var groupData = args.response?["data"]?["groupData"] as JObject;
                if (groupData == null) return;
                dynamic thisWindowGroups = new ExpandoObject();
                thisWindowGroups.dockingGroup = "";
                thisWindowGroups.snappingGroup = "";
                thisWindowGroups.topRight = false;
                foreach (var item in groupData)
                {
                    var windowsInGroup = item.Value["windowNames"] as JArray;
                    if (windowsInGroup.Where(window => (string)window == bridge.windowName).Count() > 0)
                    {
                        if ((bool)item.Value["isMovable"])
                        {
                            thisWindowGroups.dockingGroup = item.Key;
                            if ((string)item.Value["topRightWindow"] == bridge.windowName)
                            {
                                thisWindowGroups.topRight = true;
                            }
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
                    bHandled = true;

                    if (!resizing) {
                        GetWindowRect(hWnd, out windowRect);
                        resizing = true;
                        Debug.WriteLine("resize start");
                    }

                    Debug.WriteLine("resizing: " + resizing.ToString());

                    newWindowRect = (WIN32Rectangle)Marshal.PtrToStructure(lParam, typeof(WIN32Rectangle));
                    Marshal.StructureToPtr(windowRect, lParam, true);

                    resizeHandle = (int)wParam;
                                        
                    Resize(newWindowRect);
                    /*switch (resizeHandle)
                    {
                        case WMSZ_LEFT:
                            WindowResizeEndLocation.X = newWindowRect.;
                            break;
                        case WMSZ_RIGHT:
                            WindowResizeEndBottomRight.X = mousePosition.X;
                            break;
                        case WMSZ_TOP:
                            WindowResizeEndLocation.Y = mousePosition.Y;
                            break;
                        case WMSZ_BOTTOM:
                            WindowResizeEndBottomRight.Y = mousePosition.Y;
                            break;
                        case WMSZ_TOPLEFT:
                            WindowResizeEndLocation.Y = mousePosition.Y;
                            WindowResizeEndLocation.X = mousePosition.X;
                            break;
                        case WMSZ_TOPRIGHT:
                            WindowResizeEndLocation.Y = mousePosition.Y;
                            WindowResizeEndBottomRight.X = mousePosition.X;
                            break;
                        case WMSZ_BOTTOMLEFT:
                            WindowResizeEndLocation.X = mousePosition.X;
                            WindowResizeEndBottomRight.Y = mousePosition.Y;
                            break;
                        case WMSZ_BOTTOMRIGHT:
                            WindowResizeEndBottomRight.Y = mousePosition.Y;
                            WindowResizeEndBottomRight.X = mousePosition.X;
                            break;
                    }*/

                        



                    /*double scale = 1;
                    Application.Current.Dispatcher.Invoke(delegate //main thread
                    {
                        scale = (rectangle.Right - rectangle.Left) / dockingWindow.Width;
                        WindowResizeEndLocation = new Point(dockingWindow.Left, dockingWindow.Top);
                        WindowResizeEndBottomRight = new Point(dockingWindow.Left + dockingWindow.Width, dockingWindow.Top + dockingWindow.Height);
                        //Resize(WindowResizeEndLocation, WindowResizeEndBottomRight);
                    });*/

                    break;
                case WM_LBUTTONDOWN:
                    if (!resizing)
                    {
                        dockingWindow.Activate();
                        bHandled = false;
                    }
                    break;
                case WM_EXITSIZEMOVE:
                    if (resizing)
                    {
                        Debug.WriteLine("End Resize");
                        bHandled = true;
                        resizing = false;
                        Resize(newWindowRect, true);
                        dynamic props = new ExpandoObject();
                        props.windowName = dockingWindowName;
                        props.windowAction = "endMove";
                        bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                        lastResizeSent = DateTime.Now;
                        //Mouse.Capture(null);

                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            dockingWindow.Opacity = 1.0;
                        });
                    }
                    break;

            }
            return IntPtr.Zero;
        }



    }
}
