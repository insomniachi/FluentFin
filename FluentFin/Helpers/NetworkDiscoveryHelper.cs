using System.Collections.ObjectModel;
using System.Net.NetworkInformation;

namespace FluentFin.Helpers
{
	public static class NetworkDiscoveryHelper
	{
		public static string GetGatewayIp()
		{
			foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (f.OperationalStatus == OperationalStatus.Up)
				{
					foreach (GatewayIPAddressInformation d in f.GetIPProperties().GatewayAddresses)
					{
						return d.Address.ToString();
					}
				}
			}

			return string.Empty;
		}

		public static void PingAll(Action<string> deviceDetected, Action onCompleted)
		{
			var gateway = GetGatewayIp();
			string[] array = gateway.Split('.');

			Parallel.ForEach(Enumerable.Range(2, 254), number =>
			{
				var ping = new Ping();
				string ping_var = array[0] + "." + array[1] + "." + array[2] + "." + number;
				var response = ping.Send(ping_var);
				if (response.Status is IPStatus.Success)
				{
					deviceDetected(response.Address.ToString());
				}
			});

			onCompleted();
		}
	}
}
