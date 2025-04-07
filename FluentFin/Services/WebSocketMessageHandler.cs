using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.WebSockets;
using FluentFin.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Hosting;
using ReactiveUI;

namespace FluentFin.Services;

public class WebSocketMessageHandler(IObservable<IInboundSocketMessage> webSocketMessages,
									 IContentDialogService contentDialogService,
									 INavigationService navigationService,
									 IJellyfinClient jellyfinClient) : IHostedService
{
	private readonly CompositeDisposable _disposables = [];

	public Task StartAsync(CancellationToken cancellationToken)
	{
		webSocketMessages
		 .ObserveOn(RxApp.MainThreadScheduler)
		 .Subscribe(message =>
		 {
			 switch (message)
			 {
				 case GeneralCommandMessage { Data.Name: GeneralCommand_Name.DisplayMessage } gcm:
					 contentDialogService.Growl((string)gcm.Data.Arguments!.AdditionalData["Header"],
												(string)gcm.Data.Arguments!.AdditionalData["Text"],
												TimeSpan.FromMilliseconds(double.Parse((string)gcm.Data.Arguments.AdditionalData["TimeoutMs"])));
					 break;
				 case PlayQueueUpdateMessage { Data.Data.PlayingItemIndex: >= 0 } playMessage:
					 navigationService.NavigateTo<VideoPlayerViewModel>(playMessage.Data.Data);
					 break;
				 case GroupJoinedUpdateMessage groupJoined:
					 contentDialogService.Growl("", $"SyncPlay Enabled", TimeSpan.FromSeconds(5));
					 SessionInfo.GroupId = groupJoined.Data?.Data.GroupId;
					 break;
				 case GroupLeftUpdateMessage:
					 contentDialogService.Growl("", $"SyncPlay Disabled", TimeSpan.FromSeconds(5));
					 SessionInfo.GroupId = null;
					 break;
				 case UserJoinedUpdateMessage userJoined:
					 contentDialogService.Growl("", $"{userJoined.Data?.Data} joined", TimeSpan.FromSeconds(5));
					 break;
				 case UserLeftUpdateMessage userJoined:
					 contentDialogService.Growl("", $"{userJoined.Data?.Data} left", TimeSpan.FromSeconds(5));
					 break;
			 }
		 })
		 .DisposeWith(_disposables);

		Observable.Timer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1))
			.SelectMany(_ => jellyfinClient.SendWebsocketMessage(new KeepAliveMessage()).ToObservable())
			.Subscribe()
			.DisposeWith(_disposables);

		return Task.CompletedTask;
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
