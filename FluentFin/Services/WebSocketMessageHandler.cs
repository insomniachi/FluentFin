﻿using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Services;
using FluentFin.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Sdk.WebSockets;
using Microsoft.Extensions.Hosting;
using ReactiveUI;

namespace FluentFin.Services;

public class WebSocketMessageHandler(IObservable<IOutboundWebSocketMessage> webSocketMessages,
									 IContentDialogService contentDialogService,
									 INavigationService navigationService) : IHostedService
{
	private readonly CompositeDisposable _disposables = [];

	public Task StartAsync(CancellationToken cancellationToken)
	{
		webSocketMessages
		 .ObserveOn(RxSchedulers.MainThreadScheduler)
		 .Subscribe(message =>
		 {
			 switch (message)
			 {
				 case GeneralCommandMessage { Data.Name: GeneralCommand_Name.DisplayMessage } gcm:
					 contentDialogService.Growl((string)gcm.Data.Arguments!.AdditionalData["Header"],
												(string)gcm.Data.Arguments!.AdditionalData["Text"],
												TimeSpan.FromMilliseconds(double.Parse((string)gcm.Data.Arguments.AdditionalData["TimeoutMs"])));
					 break;
				 case SyncPlayGroupUpdateMessage { Data: { } update }:
					 HandleGroupUpdate(update);
					 break;
			 }
		 })
		 .DisposeWith(_disposables);

		return Task.CompletedTask;
	}

	private void HandleGroupUpdate(GroupUpdate update)
	{
		switch (update)
		{
			case { SyncPlayPlayQueueUpdate.Data: { PlayingItemIndex: >= 0 } playQueue }:
				navigationService.NavigateTo<VideoPlayerViewModel>(playQueue);
				break;
			case { SyncPlayGroupJoinedUpdate: { } joined }:
				contentDialogService.Growl("", $"SyncPlay Enabled", TimeSpan.FromSeconds(5));
				SessionInfo.GroupId = joined.Data?.GroupId;
				break;
			case { SyncPlayGroupLeftUpdate: not null }:
				contentDialogService.Growl("", $"SyncPlay Disabled", TimeSpan.FromSeconds(5));
				SessionInfo.GroupId = null;
				break;
			case { SyncPlayUserJoinedUpdate: { } userJoined }:
				contentDialogService.Growl("", $"{userJoined.Data} joined", TimeSpan.FromSeconds(5));
				break;
			case { SyncPlayUserLeftUpdate: { } userLeft }:
				contentDialogService.Growl("", $"{userLeft.Data} left", TimeSpan.FromSeconds(5));
				break;
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if (_disposables.IsDisposed)
		{
			return Task.CompletedTask;
		}

		_disposables.Dispose();

		return Task.CompletedTask;
	}
}
