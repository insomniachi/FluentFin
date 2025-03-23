﻿using FluentFin.Core.WebSockets;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace FluentFin.Services;

public class WebSocketMessageHandler(IObservable<IInboundSocketMessage> webSocketMessages,
                                     IContentDialogService contentDialogService) : IHostedService
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
