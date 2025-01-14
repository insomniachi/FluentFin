using CommunityToolkit.WinUI.Helpers;
using FluentFin.Core.Settings;
using Microsoft.UI.Xaml.Controls;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FluentFin.Helpers;

public static class FrameExtensions
{
	public static object? GetPageViewModel(this Frame frame) => frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
}


public static class SavedServerExtensions
{
	public static string GetServerUrl(this SavedServer? server)
	{
		if (server is null)
		{
			return "";
		}

		// if not internet, try local
		if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
		{
			return server.LocalUrl;
		}

		var networkNames = NetworkHelper.Instance.ConnectionInformation.NetworkNames;
		if (networkNames.Count != server.LocalNetworkNames.Count)
		{
			return server.PublicUrl;
		}

		for (int i = 0; i < networkNames.Count; i++)
		{
			if (networkNames[i] != server.LocalNetworkNames[i])
			{
				return server.PublicUrl;
			}
		}

		return server.LocalUrl;
	}

	public static bool IsLocalUrl(this string url)
	{
		Uri uri = new(url);
		var addresses = Dns.GetHostAddresses(uri.Host);
		foreach (var address in addresses)
		{
			if (IsLocalIpAddress(address))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsLocalIpAddress(IPAddress ipAddress)
	{
		byte[] bytes = ipAddress.GetAddressBytes();
		// Check for local IP ranges (e.g., 192.168.x.x, 10.x.x.x, 172.16.x.x - 172.31.x.x)
		return (bytes[0] == 10) || (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31)) || (bytes[0] == 192 && bytes[1] == 168);
	}
}

public static class SecurityExtensions
{
	public static byte[] Protect(this string plainText, byte[] entropy)
	{
		return ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), entropy, DataProtectionScope.CurrentUser);
	}

	public static string Unprotect(this byte[] protectedData, byte[] entropy)
	{
		return Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedData, entropy, DataProtectionScope.CurrentUser));
	}
}