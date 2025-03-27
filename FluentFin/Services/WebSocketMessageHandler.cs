using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Services;
using FluentFin.Core.WebSockets;
using FluentFin.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace FluentFin.Services;

public class WebSocketMessageHandler(IObservable<IInboundSocketMessage> webSocketMessages,
                                     IContentDialogService contentDialogService,
                                     INavigationService navigationService) : IHostedService
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
                 case GeneralCommandMessage { Data.Name: GeneralCommandType.DisplayMessage } gcm:
                     contentDialogService.Growl(gcm.Data.Arguments["Header"],
                                                gcm.Data.Arguments["Text"],
                                                TimeSpan.FromMilliseconds(double.Parse(gcm.Data.Arguments["TimeoutMs"])));
                     break;
                 case PlayQueueUpdateMessage { Data: not null } playMessage:
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

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if(_disposables.IsDisposed)
        {
            return Task.CompletedTask;
        }

        _disposables.Dispose();

        return Task.CompletedTask;
    }
}
