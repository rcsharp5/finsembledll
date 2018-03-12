using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class ConfigClient
    {
        private FinsembleBridge bridge;
        private RouterClient routerClient;
        public ConfigClient(FinsembleBridge bridge)
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
    }
}
