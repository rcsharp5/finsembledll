using System;
using Newtonsoft.Json.Linq;

namespace ChartIQ.Finsemble
{
	internal class Logger : IDisposable
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

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				try
				{
					// Unregister logger.
					JObject parameters = new JObject
					{
						["clientName"] = bridge.windowName
					};

					routerClient.Query("logger.service.unregister", parameters, new JObject { }, (s, a) => { });
				}
				catch
				{
					// TODO: Log error
				}

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~Logger()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
