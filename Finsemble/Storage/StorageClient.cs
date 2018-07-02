using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// The Storage client handles saving and retrieving data for your application.
    /// </summary>
    internal class StorageClient
    {
        private RouterClient routerClient;

        internal StorageClient(Finsemble bridge)
        {
            this.routerClient = bridge.RouterClient;
        }

        /// <summary>
        /// Get a value from storage.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void Get(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.Query("Storage.get", parameters, new JObject { }, callback);
        }

        /// <summary>
        /// Save a key value pair into storage.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void Save(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.Query("Storage.save", parameters, new JObject { }, callback);
        }
    }
}
