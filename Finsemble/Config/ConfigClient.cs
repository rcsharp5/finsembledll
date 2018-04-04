using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// The Config Client provides run-time access to Finsemble's configuration.
    /// </summary>
    internal class ConfigClient
    {
        private Finsemble bridge;
        private RouterClient routerClient;
        internal ConfigClient(Finsemble bridge)
        {
            this.bridge = bridge;
            this.routerClient = bridge.routerClient;
        }

        /// <summary>
        /// parameters is a JObject containing the field
        /// callback is called with the value
        /// </summary>
        /// <example>
        /// <code>
        /// bridge.configClient.GetValue(new JObject {["field"] = "fieldname"}, (sender, args) => {
        ///     var fieldValue = args.response
        /// })
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void GetValue(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.Query("configService.getValue", new JObject
            {
                ["field"] = parameters["field"]
            }, new JObject { }, callback);
        }

        /// <summary>
        /// parameters is a JObject containing the field and value
        /// callback is called with the value
        /// </summary>
        /// <example>
        /// <code>
        /// bridge.configClient.SetValue(new JObject {["field"] = "fieldname", ["value"] = "value"})
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void SetValue(JObject parameters)
        {
            routerClient.Query("configService.setValue", new JObject
            {
                ["field"] = parameters["field"],
                ["value"] = parameters["value"]
            }, new JObject { }, (s, a) => { });

        }
    }
}
