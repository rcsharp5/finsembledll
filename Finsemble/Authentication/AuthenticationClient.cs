using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// The Authentication Client API provides hooks for plugging in a custom sign-on component at the beginning of Finsemble start-up (before application-level components are started).
    /// </summary>
    public class AuthenticationClient
    {
        private Finsemble bridge;
        private RouterClient routerClient;

        internal AuthenticationClient(Finsemble bridge)
        {
            this.bridge = bridge;
            routerClient = bridge.RouterClient;
        }

        /// <summary>
        /// During Finsemble's start-up process, this function must be invoked before Finsemble will start the application.
        /// Once invoked, the authenticated user name and authorization credentials are received by the Authentication Service and published on the "AuthenticationService.authorization" channel.
        /// Any component can revieve the credentials by subscribing to that channel or by calling GetCurrentCredentials.
        /// Note that all calls to Storage Client are keyed to the authenticated *user*. See StorageClient.SetUser.
        /// If authentication is not enabled, then "defaultUser" is used instead.
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="credentials">Object containing user credentials</param>
        public void PublishAuthorization<T>(string user, T credentials)
        {
            routerClient.Transmit("AuthenticationService.authorization", new JObject
            {
                ["user"] = user,
                ["credentials"] = JObject.FromObject(credentials)
            });
        }

        /// <summary>
        /// Returns the current global credentials (as published through PublishAuthorization}) or null if no credentials are set yet.
        /// </summary>
        /// <param name="callback">A function that returns the current credentials. Will return null if no credentials have yet been established.</param>
        public void GetCurrentCredentials<T>(EventHandler<T> callback)
        {
            routerClient.Query("authentication.currentCredentials", new JObject { }, new JObject { }, (sender, args) => {
                var jCredentials = args.response?["data"];
                T credentials = jCredentials.ToObject<T>();
                callback(sender, credentials);
            });
        }

    }
}
