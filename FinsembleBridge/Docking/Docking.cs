using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ChartIQ.Finsemble
{
    public class Docking
    {
        FinsembleBridge bridge;
        string dockingChannel;
        dynamic dockingWindow;
        string dockingWindowName;
        bool moving = false;
        Point startPosition;

        public Docking(FinsembleBridge _bridge)
        {
            this.bridge = _bridge;
        }

        public void Register(Window window, string windowName, string channel)
        {
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
        }

        private void Got_Docking_Message(string sourceUuid, string topic, object message)
        {
            var joMessage = message as JObject;
            var action = joMessage.GetValue("action").ToString();
            var jsonMessage = joMessage.GetValue("bounds") as JObject;

            switch (action)
            {
                case "setBounds":
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        dockingWindow.Top = Double.Parse(jsonMessage.GetValue("top").ToString());
                        dockingWindow.Left = Double.Parse(jsonMessage.GetValue("left").ToString());
                        dockingWindow.Height = Double.Parse(jsonMessage.GetValue("height").ToString());
                        dockingWindow.Width = Double.Parse(jsonMessage.GetValue("width").ToString()); ;
                    });
                    break;
            }
        }

        public void Minimize()
        {
            dockingWindow.WindowState = WindowState.Minimized;
            dynamic props = new ExpandoObject();
            props.windowName = dockingWindowName;
            props.windowAction = "minimize";
            bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
        }

        public void Maxmimize()
        {
            dockingWindow.WindowState = WindowState.Maximized;
            dynamic props = new ExpandoObject();
            props.windowName = dockingWindowName;
            props.windowAction = "maximize";
            bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
        }

        public void Restore()
        {
            dockingWindow.WindowState = WindowState.Normal;
            dynamic props = new ExpandoObject();
            props.windowName = dockingWindowName;
            props.windowAction = "restore";
            bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
        }

        public void Hide()
        {
            dockingWindow.Hide();
            dynamic props = new ExpandoObject();
            props.windowName = dockingWindowName;
            props.windowAction = "hide";
            bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
        }

        public void Show()
        {
            dockingWindow.Show();
            dynamic props = new ExpandoObject();
            props.windowName = dockingWindowName;
            props.windowAction = "show";
            bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
        }

        public void BringToFront()
        {
            dockingWindow.BringIntoView();
            dynamic props = new ExpandoObject();
            props.windowName = dockingWindowName;
            props.windowAction = "bringToFront";
            bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
        }

        public void Close()
        {
            dynamic props = new ExpandoObject();
            props.windowName = dockingWindowName;
            props.windowAction = "close";
            bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
        }

        public void StartMove(dynamic sender, MouseButtonEventArgs e)
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
        }

        public void Move(object sender, MouseEventArgs e)
        {
            if (moving)
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
            }

        }

        public void EndMove(object sender, MouseButtonEventArgs e)
        {
            dynamic props = new ExpandoObject();
            props.windowName = dockingWindowName;
            props.windowAction = "endMove";
            bridge.SendRPCCommand("NativeWindow", JObject.FromObject(props).ToString(), this.dockingChannel);
            moving = false;
            Mouse.Capture(null);
        }
    }
}
