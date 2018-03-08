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

        public void publishAuthorization(string user, JObject credentials)
        {
            routerClient.transmit("AuthenticationService.authorization", new JObject
            {
                ["user"] = user,
                ["credentials"] = credentials
            });
        }
        
    }
}
