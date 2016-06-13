using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Net;

namespace MonoTools.Debugger.Library {

	public class TraceRoute {

		private const string Data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

		public static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress) {
			return GetTraceRoute(hostNameOrAddress, 1);
		}
		private static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress, int ttl) {
			Ping pinger = new Ping();
			PingOptions pingerOptions = new PingOptions(ttl, true);
			int timeout = 10000;
			byte[] buffer = Encoding.ASCII.GetBytes(Data);
			PingReply reply = default(PingReply);

			reply = pinger.Send(hostNameOrAddress, timeout, buffer, pingerOptions);

			List<IPAddress> result = new List<IPAddress>();
			if (reply.Status == IPStatus.Success) {
				result.Add(reply.Address);
			} else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut) {
				//add the currently returned address if an address was found with this TTL
				if (reply.Status == IPStatus.TtlExpired) result.Add(reply.Address);
				//recurse to get the next address...
				IEnumerable<IPAddress> tempResult = default(IEnumerable<IPAddress>);
				tempResult = GetTraceRoute(hostNameOrAddress, ttl + 1);
				result.AddRange(tempResult);
			} else {
				//failure 
			}

			return result;
		}
	}
}