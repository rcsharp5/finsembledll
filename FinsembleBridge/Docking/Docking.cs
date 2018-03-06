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
        string dockingChannel;
        dynamic dockingWindow;
        string dockingWindowName;
        bool moving = false;
        bool resizing = false;
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

        double dpiX, dpiY;

        Timer resizeTimer = new Timer(250);

        public Docking(FinsembleBridge _bridge, Window window, string windowName, string channel)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                this.bridge = _bridge;
                resizeTimer.Elapsed += handleResizeEnd;
                dynamic props = new ExpandoObject();
                props.windowName = windowName;
                props.top = window.Top;
                props.left = window.Left;
                props.width = window.Width;
                props.height = window.Height;
                props.windowAction = "open";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), channel);
                this.dockingChannel = channel;
                this.dockingWindow = window;
                this.dockingWindowName = windowName;
                bridge.SubscribeToChannel(dockingChannel, Got_Docking_Message);
            });
        }

        private void handleResizeEnd(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                resizeTimer.Stop();
                Resize(WindowResizeEndLocation, WindowResizeEndBottomRight);
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "endMove";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                resizing = false;
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
                        dockingWindow.Top = Double.Parse(jsonMessage.GetValue("top").ToString());
                        dockingWindow.Left = Double.Parse(jsonMessage.GetValue("left").ToString());
                        dockingWindow.Height = Double.Parse(jsonMessage.GetValue("height").ToString());
                        dockingWindow.Width = Double.Parse(jsonMessage.GetValue("width").ToString());
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
                case "groupUpdate":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        jsonMessage = joMessage.GetValue("groupData") as JObject;
                        dynamic groupData = new ExpandoObject();
                        groupData.dockingGroup = jsonMessage.GetValue("dockingGroup").ToString();
                        groupData.snappingGroup = jsonMessage.GetValue("snappingGroup").ToString();
                        dockingWindow.Docking_GroupUpdate(groupData);
                    });
                    break;
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
                        dockingWindow.GotFinsembleClose();
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

        public void Move(object sender, MouseEventArgs e)
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
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        public void LeaveGroup(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "leaveGroup";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            });
        }

        public void Resize (Point TopCorner, Point BottomCorner)
        {
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
            });
        }

        public void Window_Loaded()
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
            });
        }

        public void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate //main thread
            {
                if (!resizing) return;
                dynamic props = new ExpandoObject();
                props.windowName = dockingWindowName;
                props.windowAction = "endMove";
                bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
                resizing = false;
                Mouse.Capture(null);
            });
        }

        //https://stackoverflow.com/questions/12376141/intercept-a-move-event-in-a-wpf-window-before-the-move-happens-on-the-screen
        private IntPtr HwndMessageHook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool bHandled)
        {
            switch (msg)
            {
                case WM_SIZING:
                    Mouse.Capture(dockingWindow); //this isn't working yet - TODO - for figuring out when resize has ended

                    resizing = true;
                    int sizeType = (int)wParam;
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
                    Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                    {
                        WindowResizeEndLocation = new Point(rectangle.Left / scale, rectangle.Top / scale);
                        WindowResizeEndBottomRight = new Point(rectangle.Right / scale, rectangle.Bottom / scale);
                        rectangle.Left = Convert.ToInt32(WindowLocation.X * scale);
                        rectangle.Top = Convert.ToInt32(WindowLocation.Y * scale);
                        rectangle.Bottom = Convert.ToInt32(WindowBottomRight.Y * scale);
                        rectangle.Right = Convert.ToInt32(WindowBottomRight.X * scale);
                        Marshal.StructureToPtr(rectangle, lParam, true);
                        Resize(WindowResizeEndLocation, WindowResizeEndBottomRight);
                        lastResizeSent = DateTime.Now;
                        resizeTimer.Stop();
                        resizeTimer.Start();
                    });
                    TimeSpan t = DateTime.Now - lastResizeSent;
                    if (t.TotalMilliseconds < 20) return IntPtr.Zero;

                    break;

            }
            return IntPtr.Zero;
        }
    }
}
