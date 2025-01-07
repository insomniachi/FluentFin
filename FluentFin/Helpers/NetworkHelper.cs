using Windows.Networking.Connectivity;

namespace FluentFin.Helpers;

public class NetworkHelper
{
	/// <summary>
	/// Event raised when the network changes.
	/// </summary>
	public event EventHandler NetworkChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="NetworkHelper"/> class.
	/// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	protected NetworkHelper()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		ConnectionInformation = new ConnectionInformation();

		UpdateConnectionInformation();

		NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
	}

	/// <summary>
	/// Finalizes an instance of the <see cref="NetworkHelper"/> class.
	/// </summary>
	~NetworkHelper()
	{
		NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
	}

	/// <summary>
	/// Gets public singleton property.
	/// </summary>
	public static NetworkHelper Instance { get; } = new NetworkHelper();

	/// <summary>
	/// Gets instance of <see cref="ConnectionInformation"/>.
	/// </summary>
	public ConnectionInformation ConnectionInformation { get; }

	/// <summary>
	/// Checks the current connection information and raises <see cref="NetworkChanged"/> if needed.
	/// </summary>
	private void UpdateConnectionInformation()
	{
		lock (ConnectionInformation)
		{
			try
			{
				ConnectionInformation.UpdateConnectionInformation(NetworkInformation.GetInternetConnectionProfile());

				NetworkChanged?.Invoke(this, EventArgs.Empty);
			}
			catch
			{
				ConnectionInformation.Reset();
			}
		}
	}

	/// <summary>
	/// Invokes <see cref="UpdateConnectionInformation"/> when the current network status changes.
	/// </summary>
	private void OnNetworkStatusChanged(object sender)
	{
		UpdateConnectionInformation();
	}
}

public class ConnectionInformation
{
	private readonly List<string> networkNames = new();

	/// <summary>
	/// Updates  the current object based on profile passed.
	/// </summary>
	/// <param name="profile">instance of <see cref="ConnectionProfile"/></param>
	public void UpdateConnectionInformation(ConnectionProfile profile)
	{
		if (profile == null)
		{
			Reset();

			return;
		}

		networkNames.Clear();

		uint ianaInterfaceType = profile.NetworkAdapter?.IanaInterfaceType ?? 0;

		switch (ianaInterfaceType)
		{
			case 6:
				ConnectionType = ConnectionType.Ethernet;
				break;

			case 71:
				ConnectionType = ConnectionType.WiFi;
				break;

			case 243:
			case 244:
				ConnectionType = ConnectionType.Data;
				break;

			default:
				ConnectionType = ConnectionType.Unknown;
				break;
		}

		var names = profile.GetNetworkNames();
		if (names?.Count > 0)
		{
			networkNames.AddRange(names);
		}

		ConnectivityLevel = profile.GetNetworkConnectivityLevel();

		switch (ConnectivityLevel)
		{
			case NetworkConnectivityLevel.None:
			case NetworkConnectivityLevel.LocalAccess:
				IsInternetAvailable = false;
				break;

			default:
				IsInternetAvailable = true;
				break;
		}

		ConnectionCost = profile.GetConnectionCost();
		SignalStrength = profile.GetSignalBars();
	}

	/// <summary>
	/// Resets the current object to default values.
	/// </summary>
	internal void Reset()
	{
		networkNames.Clear();

		ConnectionType = ConnectionType.Unknown;
		ConnectivityLevel = NetworkConnectivityLevel.None;
		IsInternetAvailable = false;
		ConnectionCost = null;
		SignalStrength = null;
	}

	/// <summary>
	/// Gets a value indicating whether if the current internet connection is metered.
	/// </summary>
	public bool IsInternetOnMeteredConnection
	{
		get
		{
			return ConnectionCost != null && ConnectionCost.NetworkCostType != NetworkCostType.Unrestricted;
		}
	}

	/// <summary>
	/// Gets a value indicating whether internet is available across all connections.
	/// </summary>
	/// <returns>True if internet can be reached.</returns>
	public bool IsInternetAvailable { get; private set; }

	/// <summary>
	/// Gets connection type for the current Internet Connection Profile.
	/// </summary>
	/// <returns>value of <see cref="ConnectionType"/></returns>
	public ConnectionType ConnectionType { get; private set; }

	/// <summary>
	/// Gets connectivity level for the current Internet Connection Profile.
	/// </summary>
	/// <returns>value of <see cref="NetworkConnectivityLevel"/></returns>
	public NetworkConnectivityLevel ConnectivityLevel { get; private set; }

	/// <summary>
	/// Gets connection cost for the current Internet Connection Profile.
	/// </summary>
	/// <returns>value of <see cref="NetworkConnectivityLevel"/></returns>
	public ConnectionCost? ConnectionCost { get; private set; }

	/// <summary>
	/// Gets signal strength for the current Internet Connection Profile.
	/// </summary>
	/// <returns>value of <see cref="NetworkConnectivityLevel"/></returns>
	public byte? SignalStrength { get; private set; }

	/// <summary>
	/// Gets the network names associated with the current Internet Connection Profile.
	/// </summary>
	/// <returns>value of <see cref="IReadOnlyList{String}"/></returns>
	public IReadOnlyList<string> NetworkNames
	{
		get
		{
			return networkNames.AsReadOnly();
		}
	}
}

public enum ConnectionType
{
	/// <summary>
	/// Connected to wired network
	/// </summary>
	Ethernet,

	/// <summary>
	/// Connected to wireless network
	/// </summary>
	WiFi,

	/// <summary>
	/// Connected to mobile data connection
	/// </summary>
	Data,

	/// <summary>
	/// Connection type not identified
	/// </summary>
	Unknown,
}
