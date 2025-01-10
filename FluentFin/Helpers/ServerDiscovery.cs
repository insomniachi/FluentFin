using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FluentFin.Helpers;

public class ServerDiscovery
{
	private const string DiscoveryMessage = "Who is JellyfinServer?";
	private const int DiscoveryPort = 7359;
	private static readonly TimeSpan TimeOut = TimeSpan.FromSeconds(5);

	public static async Task DiscoverServersAsync(Action<DiscoveryInfo> onDiscovered, Action onCompleted)
	{
		var udpClient = new UdpClient
		{
			EnableBroadcast = true
		};

		var messageBytes = Encoding.UTF8.GetBytes(DiscoveryMessage);
		var broadcastAddress = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

		await udpClient.SendAsync(messageBytes, messageBytes.Length, broadcastAddress);

		var receiveTask = Task.Run(async () =>
		{
			while (true)
			{
				var result = await udpClient.ReceiveAsync();
				var response = Encoding.UTF8.GetString(result.Buffer);
				var discoveryInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<DiscoveryInfo>(response);
				if (discoveryInfo is null)
				{
					return;
				}
				onDiscovered(discoveryInfo);
			}
		});

		await Task.WhenAny(receiveTask, Task.Delay(TimeOut));
		onCompleted();
		udpClient.Close();
	}
}