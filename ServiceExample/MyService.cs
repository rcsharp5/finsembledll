using System;
using System.ServiceProcess;
using ChartIQ.Finsemble;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ServiceExample
{
    internal class MyService : ServiceBase
    {
        private Finsemble FSBL;

        internal MyService(String[] args)
        {
            this.ServiceName = "MyService";
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.AutoLog = false;
            FSBL = new Finsemble(args, null);
        }

        protected override void OnStart(string[] args)
        {
            FSBL.Connect();
            FSBL.Connected += FSBL_Connected;
        }

        private void FSBL_Connected(object sender, EventArgs e)
        {
            FSBL.RPC("Logger.error", new List<JToken> {
                "Error Test"
            });
        }

        protected override void OnStop()
        {
            // TODO: add shutdown stuff
        }

    }
}
