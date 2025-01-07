using FluentFin.Core.Settings;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Helpers;

public static class FrameExtensions
{
    public static object? GetPageViewModel(this Frame frame) => frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
}


public static class SavedServerExtensions
{
    public static string GetServerUrl(this SavedServer? server)
    {
        if(server is null)
        {
            return "";
        }

        // if not internet, try local
        if(!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
        {
            return server.LocalUrl;
        }

        var networkNames = NetworkHelper.Instance.ConnectionInformation.NetworkNames;
        if(networkNames.Count != server.LocalNetworkNames.Count)
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
}