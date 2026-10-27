﻿using System.Reactive.Disposables;
using System.Reactive.Linq;
using Jellyfin.Sdk.WebSockets;
using Microsoft.Extensions.Logging;

namespace FluentFin.Core.Services;

public partial class JellyfinClient
{
	private JellyfinWebSocketClient? _webSocket;

	public IObservable<T> ObserveWebSocket<T>() where T : class, IOutboundWebSocketMessage, new()
	{
		return Observable.Create<T>(observer => _webSocket?.Subscribe<T>(observer.OnNext) ?? Disposable.Empty);
	}

	private void StartWebSocketConnection()
	{
		// settings are recreated on every login, so the socket client is too
		_webSocket?.Dispose();
		_webSocket = new JellyfinWebSocketClient(_settings, new JellyfinWebSocketOptions
		{
			PeriodicMessageInterval = TimeSpan.FromSeconds(1.5),
			IdleTimeout = Timeout.InfiniteTimeSpan
		});
		_webSocket.ErrorOccurred += (_, e) => logger.LogError(e.Content, "WebSocket error");
		_webSocket.StateChanged += (_, e) => logger.LogInformation(e.Error, "WebSocket {State}", e.State);
		_webSocket.SubscribeAll(socketMessageSender.OnNext);
	}
}
