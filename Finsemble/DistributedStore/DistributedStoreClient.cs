﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// The data store client handles creating/retrieving global distributed stores. Local stores are not supported. This data is not persisted. You can add listeners at multiple levels (store or field) and get the updated data as it's updated in the store. Fields are stored within the store as key/value pair.
    /// </summary>
    internal class DistributedStoreClient
    {
        Finsemble bridge;
        internal DistributedStoreClient(Finsemble bridge)
        {
            this.bridge = bridge;
        }

        /// <summary>
        /// This returns a StoreModel which is used to get and set values on a global store and create listeners on specific values. Only global stores are supported.
        /// </summary>
        /// <example>
        /// <code>
        /// bridge.distributedStoreClient.GetStore(new JObject {
        ///     ["store"] = "storeName",
        ///     ["global"] = true
        /// }, (sender, store) {
        ///     // Do something with the store.
        /// })
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="args"></param>
        public void GetStore(JObject parameters, EventHandler<StoreModel> args)
        {
            EventHandler<FinsembleEventArgs> handler = delegate (object sender, FinsembleEventArgs e)
            {
                var store = new StoreModel(e.response["data"] as JObject, bridge);
                args.Invoke(this, store);
            };
            bridge.RouterClient.Query("storeService.getStore", parameters, new JObject { }, handler );
        }

        /// <summary>
        /// This creates a global distributed store or returns an existing one if it already exists. The StoreModel returned can be used to get and set values on a global store and create listeners on specific values. Only global stores are supported.
        /// </summary>
        /// <example>
        /// <code>
        /// bridge.distributedStoreClient.GetStore(new JObject {
        ///     ["store"] = "storeName",
        ///     ["global"] = true
        /// }, (sender, store) {
        ///     // Do something with the store.
        /// })
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="args"></param>
        public void CreateStore(JObject parameters, EventHandler<StoreModel> args)
        {
            EventHandler<FinsembleEventArgs> handler = delegate (object sender, FinsembleEventArgs e)
            {
                var store = new StoreModel(e.response["data"] as JObject, bridge);
                args.Invoke(this, store);
            };
            bridge.RouterClient.Query("storeService.createStore", parameters, new JObject { }, handler);
        }



    }
}
