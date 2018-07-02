using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    internal class Logger
    {
        private RouterClient routerClient;
        private Finsemble bridge;

        public Logger(Finsemble bridge)
        {
            routerClient = bridge.RouterClient;
            this.bridge = bridge;
            routerClient.Query("logger.service.register", new JObject
            {
                ["clientName"] = bridge.windowName,
                ["clientChannel"] = "finsemble.logger.client." + bridge.windowName,
                ["uuid"] = bridge.uuid,
                ["windowName"] = bridge.windowName
            }, new JObject { }, (s, a) => { });
            bridge.window.Closing += Window_Closing;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            routerClient.Query("logger.service.unregister", new JObject { ["clientName"] = bridge.windowName }, new JObject { }, (s, a) => { });
        }

        private void formatAndSendMessage(string category, string type, params JToken[] args)
        {
            var message = new JObject
            {
                ["category"] = category,
                ["logClientName"] = bridge.windowName,
                ["logType"] = type,
                ["logData"] = JArray.FromObject(args).ToString(),
                ["logTimestamp"] = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds
            };
            routerClient.Transmit("logger.service.logMessages", new JArray
            {
                message
            });
        }

        private JToken[] AddStackTrace(JToken[] message, string stackTrace)
        {
            JToken[] args = new JToken[message.Length + 1];
            int i;
            for (i = 0; i < message.Length; i++)
            {
                args[i] = message[i];
            }
            args[i] = stackTrace;
            return args;
        }

        public void Log(params JToken[] message)
        {
            formatAndSendMessage("dev", "Log", message);
        }

        public void Debug(params JToken[] message)
        {
            var args = AddStackTrace(message, Environment.StackTrace);
            formatAndSendMessage("dev", "Debug", args);
        }

        public void Error(params JToken[] message)
        {
            var args = AddStackTrace(message, Environment.StackTrace);
            formatAndSendMessage("dev", "Error", args);
        }

        public void Warn(params JToken[] message)
        {
            formatAndSendMessage("dev", "Warn", message);
        }

        public void Info(params JToken[] message)
        {
            formatAndSendMessage("dev", "Info", message);
        }

        public void Verbose(params JToken[] message)
        {
            formatAndSendMessage("dev", "Verbose", message);
        }
    }
}
