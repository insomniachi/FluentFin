using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentFin.Core.WebSockets;
using FluentFin.Core.WebSockets.Messages;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;

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

		if (!Enum.TryParse<SessionMessageType>(type, out var messageType))
		{
			return;
		}

		if (messageType is SessionMessageType.KeepAlive or SessionMessageType.ForceKeepAlive)
		{
			return;
		}

		if (messageType is SessionMessageType.SyncPlayGroupUpdate)
		{
			var data = document.RootElement.GetProperty("Data");
			var grupUpdateType = data.GetProperty("Type").GetString();
			if (!Enum.TryParse<GroupUpdateType>(grupUpdateType, out var updateType))
			{
				return;
			}

			if (updateType.Parse(socketMessage) is IInboundSocketMessage message)
			{
				socketMessageSender.OnNext(message);
			}

		}
		else if (messageType.Parse(document) is IInboundSocketMessage message)
		{
			socketMessageSender.OnNext(message);
		}

		inputStream.Dispose();

	}
}


internal static class MessageConverter
{
	private static readonly KiotaJsonSerializationContext _context = new(new JsonSerializerOptions(KiotaJsonSerializationContext.Default.Options)
	{
		Converters =
		{
			new JsonNullableGuidConverter(),
			new JsonGuidConverter()
		}
	});

	internal static WebSocketMessage? Parse(this SessionMessageType messageType, JsonDocument doc)
	{
		try
		{
			return messageType switch
			{
				SessionMessageType.GeneralCommand => KiotaParseObject<GeneralCommandMessage, GeneralCommand>(doc),
				SessionMessageType.Playstate => doc.Deserialize<PlayStateMessage>(),
				SessionMessageType.UserDataChanged => KiotaParseObject<UserDataChangeMessage, UserDataChangeInfo>(doc),
				SessionMessageType.SyncPlayCommand => doc.Deserialize<SyncPlayCommandMessage>(),
				SessionMessageType.Sessions => KiotaParseList<SessionInfoMessage, SessionInfoDto>(doc),
				_ => null
			};
		}
		catch(Exception ex)
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

	private static TMessage KiotaParseObject<TMessage,TData>(JsonDocument doc) 
		where TData : IParsable, new()
		where TMessage : WebSocketMessage<TData>, new()
	{
		var id = doc.RootElement.GetProperty("MessageId").GetString();
		var data = doc.RootElement.GetProperty("Data");
		var parseNode = new JsonParseNode(data, _context);

		return new TMessage
		{
			MessageId = Guid.TryParse(id, out var guid) ? guid : Guid.Empty,
			Data = parseNode.GetObjectValue(CreateFromDiscriminatorValue<TData>)
		};
	}

	private static TMessage KiotaParseList<TMessage,TData>(JsonDocument doc)
		where TData : IParsable, new()
		where TMessage : WebSocketMessage<List<TData>>, new()
	{
		var id = doc.RootElement.GetProperty("MessageId").GetString();
		var array = doc.RootElement.GetProperty("Data");
		List<TData?> data = [];

		foreach (var item in array.EnumerateArray())
		{
			var text = item.GetRawText();
			var parseNode = new JsonParseNode(item, _context);
			data.Add(parseNode.GetObjectValue(CreateFromDiscriminatorValue<TData>));
		}

		return new TMessage
		{
			MessageId = Guid.TryParse(id, out var guid) ? guid : Guid.Empty,
			Data = [.. data.Where(x => x is not null)]
		};
	}

	private static T CreateFromDiscriminatorValue<T>(IParseNode parseNode)
		where T : IParsable, new()
	{
		return new T();
	}
}