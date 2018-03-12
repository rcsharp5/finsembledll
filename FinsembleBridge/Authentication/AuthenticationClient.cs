using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class AuthenticationClient
    {
        private FinsembleBridge bridge;
        private RouterClient routerClient;

        public AuthenticationClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            routerClient = bridge.routerClient;
        }

        /// <summary>
        /// During Finsemble's start-up process, this function must be invoked before Finsemble will start the application.
        /// Once invoked, the authenticated user name and authorization credentials are received by the Authentication Service and published on the "AuthenticationService.authorization" channel.
        /// Any component can revieve the credentials by subscribing to that channel or by calling {@link AuthenticationClient#getCurrentCredentials}.
        /// Note that all calls to Storage Client are keyed to the authenticated *user*. See {@link StorageClient#setUser}.
        /// If authentication is not enabled, then "defaultUser" is used instead.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="credentials"></param>
        public void PublishAuthorization(string user, JObject credentials)
        {
            routerClient.Transmit("AuthenticationService.authorization", new JObject
            {
                ["user"] = user,
                ["credentials"] = credentials
            });
        }

    }
}
