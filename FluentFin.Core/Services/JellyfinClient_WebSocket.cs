using FluentFin.Core.WebSockets;
using FluentFin.Core.WebSockets.Messages;
using Flurl;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text.Json;

namespace FluentFin.Core.Services;

public partial class JellyfinClient
{
	private async Task CreateSocketConnection(CancellationToken ct)
	{
		var socket = new ClientWebSocket();
		socket.Options.SetRequestHeader("Authorization", _settings.GetAuthorizationHeader());

		try
		{
			await socket.ConnectAsync(new Uri(BaseUrl.Replace("http", "ws").AppendPathSegment("/socket")), ct);
			await Task.Factory.StartNew(async () => await ReceiveLoop(socket, ct), ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	private async Task ReceiveLoop(WebSocket socket, CancellationToken token)
	{
		var loopToken = token;
		MemoryStream? outputStream = null;
		WebSocketReceiveResult receiveResult;
		var buffer = new byte[8192];
		try
		{
			while (!loopToken.IsCancellationRequested)
			{
				outputStream = new MemoryStream(8192);
				do
				{
					receiveResult = await socket.ReceiveAsync(buffer, token);
					if (receiveResult.MessageType != WebSocketMessageType.Close)
					{
						outputStream.Write(buffer, 0, receiveResult.Count);
					}
				}
				while (!receiveResult.EndOfMessage);

				if (receiveResult.MessageType == WebSocketMessageType.Close)
				{
					break;
				}

				outputStream.Position = 0;
				await ResponseReceived(outputStream);
			}
		}
		catch (TaskCanceledException) { }
		finally
		{
			outputStream?.Dispose();
		}
	}

	private async Task ResponseReceived(Stream inputStream)
	{
		using var reader = new StreamReader(inputStream);
		var socketMessage = await reader.ReadToEndAsync();

		using var document = JsonDocument.Parse(socketMessage);
		var type = document.RootElement.GetProperty("MessageType").GetString();

		if(!Enum.TryParse<SessionMessageType>(type, out var messageType))
		{
			return;
		}

		if(messageType is SessionMessageType.KeepAlive or SessionMessageType.ForceKeepAlive)
		{
			return;
		}

		if(messageType is SessionMessageType.SyncPlayGroupUpdate)
		{
			var data = document.RootElement.GetProperty("Data");
			var grupUpdateType = data.GetProperty("Type").GetString();
			if(!Enum.TryParse<GroupUpdateType>(grupUpdateType, out var updateType))
			{
				return;
			}

            if (updateType.Parse(socketMessage) is IInboundSocketMessage message)
            {
				socketMessageSender.OnNext(message);
            }

        }
		else if (messageType.Parse(socketMessage) is IInboundSocketMessage message)
		{
			socketMessageSender.OnNext(message);
		}

		inputStream.Dispose();

	}
}


internal static class MessageConverter
{
	internal static WebSocketMessage? Parse(this SessionMessageType messageType, string json)
	{
		try
		{
			return messageType switch
			{
				SessionMessageType.GeneralCommand => JsonSerializer.Deserialize<GeneralCommandMessage>(json),
				SessionMessageType.Playstate => JsonSerializer.Deserialize<PlayStateMessage>(json),
				SessionMessageType.UserDataChanged => JsonSerializer.Deserialize<UserDataChangeMessage>(json),
				SessionMessageType.SyncPlayCommand => JsonSerializer.Deserialize<SyncPlayCommandMessage>(json),
				_ => null
			};
		}
		catch
		{
			return null;
		}
	}

    internal static WebSocketMessage? Parse(this GroupUpdateType messageType, string json)
    {
        try
        {
            return messageType switch
            {
				GroupUpdateType.PlayQueue => JsonSerializer.Deserialize<PlayQueueUpdateMessage>(json),
				GroupUpdateType.UserJoined => JsonSerializer.Deserialize<UserJoinedUpdateMessage>(json),
				GroupUpdateType.UserLeft => JsonSerializer.Deserialize<UserLeftUpdateMessage>(json),
                GroupUpdateType.GroupJoined => JsonSerializer.Deserialize<GroupJoinedUpdateMessage>(json),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

}