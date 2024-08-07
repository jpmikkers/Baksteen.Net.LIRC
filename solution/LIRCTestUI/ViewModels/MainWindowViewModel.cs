﻿namespace LIRCTestUI.ViewModels;

using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Baksteen.Net.LIRC;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LIRCTestUI.Tools;
using LIRCTestUI.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

// use NotifyDataErrorInfo for mvvmct validation, see https://github.com/AvaloniaUI/Avalonia/issues/8397
// then on Apply/OK buttons you can bind IsEnabled to !HasErrors
public partial class MainWindowViewModel : ObservableValidator
{
    private ILIRCClient? _client;

    [ObservableProperty]
    private bool _useUnixDomainSocket = true;

    [ObservableProperty]
    private string[] _tcpipEndPointCompletions =
    [
        "127.0.0.1:8765",
        "[::1]:8765",
        "localhost:8765",
        "raspberrypi:8765"
    ];

    [ObservableProperty]
    private string[] _unixEndPointCompletions =
    [
        "/var/run/lirc/lircd"
    ];

    [ObservableProperty]
    [Required(AllowEmptyStrings = false, ErrorMessage = "Endpoint is required")]
    [NotifyDataErrorInfo]
    [EndPointValidation]
    private string _iPEndPointAsString = "localhost:8765";

    [ObservableProperty]
    [Required(AllowEmptyStrings = false, ErrorMessage = "Address is required")]
    [NotifyDataErrorInfo]
    private string _unixEndPointAsString = "/var/run/lirc/lircd";

    [ObservableProperty]
    private IRRemote? _selectedRemote = null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearMarksCommand))]
    private bool _connected;

    public MainWindowViewModel()
    {
    }

    private async Task HandleLIRCEvent(LIRCEvent ev)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            switch(ev.Event)
            {
                case LIRCEvent.EventType.ReceivedButton when ev.Button is not null:
                    {
                        var irRemote = RemoteList.FirstOrDefault(x => x.Name == ev.Button.ButtonInfo.RemoteControl);

                        if(SelectedRemote != irRemote)
                        {
                            SelectedRemote = irRemote;
                        }

                        if(irRemote is not null)
                        {
                            var irButton = irRemote.ButtonList.FirstOrDefault(x => x.Name == ev.Button.ButtonInfo.Button);

                            if(irButton is not null)
                            {
                                irButton.IsSeen = true;
                                await irButton.TriggerAnimation();

                                // scroll button into view
                                if(App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                                {
                                    if(lifetime.MainWindow is MainWindow mainWindow)
                                    {
                                        mainWindow.ScrollRelatedButtonIntoView(irButton);
                                    }
                                }
                            }
                        }

                        break;
                    }

                case LIRCEvent.EventType.Disconnected:
                    await Disconnect();
                    if(ev.Reason is not null) await ShowExceptionDialog(ev.Reason);
                    break;

                case LIRCEvent.EventType.Sighup:
                    // the daemon reloaded its remote configurations, so lets (re-)populate the remote list
                    await PopulateRemotes();
                    break;
            }
        }, DispatcherPriority.Background).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await Task.CompletedTask;
    }

    [ObservableProperty]
    private AvaloniaList<IRRemote> _remoteList = [];

    private bool CanConnect => !Connected;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task Connect()
    {
        try
        {
            EndPoint endPoint;
            RemoteList.Clear();

            if(UseUnixDomainSocket)
            {
                endPoint = new UnixDomainSocketEndPoint(UnixEndPointAsString);
            }
            else
            {
                var uri = EndPointValidationAttribute.ConvertToUri(IPEndPointAsString);
                var addresses = await Dns.GetHostAddressesAsync(uri.Host);

                var address = addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                address ??= addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);

                if(address is null)
                {
                    throw new InvalidOperationException("Could not resolve ip address");
                }

                endPoint = new IPEndPoint(address, uri.Port);
            }

            _client = await LIRCClient.Connect(
                endPoint: endPoint,
                settings: new()
                {
                    OnLIRCEventAsync = HandleLIRCEvent
                }
            );

            await PopulateRemotes();
            Connected = true;
        }
        catch(Exception ex)
        {
            await ShowExceptionDialog(ex);
        }
    }

    private async Task PopulateRemotes()
    {
        if(_client is not null)
        {
            RemoteList.Clear();

            foreach(var remote in await _client.ListRemoteControls())
            {
                var irRemote = new IRRemote { Name = remote };

                foreach(var button in await _client.ListRemoteControlKeys(remote))
                {
                    irRemote.ButtonList.Add(new IRButton { RemoteName = remote, Name = button.Button });
                }

                RemoteList.Add(irRemote);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(Connected))]
    private async Task Disconnect()
    {
        if(_client is not null)
        {
            try
            {
                await _client.DisposeAsync();
            }
            catch
            {
            }
            finally
            {
                Connected = false;
                RemoteList.Clear();
                _client = null;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(Connected))]
    private void ClearMarks()
    {
        foreach(var remote in RemoteList)
        {
            foreach(var button in remote.ButtonList)
            {
                button.IsSeen = false;
            }
        }
    }

    [RelayCommand]
    private async Task SendOnce(IRButton? button)
    {
        try
        {
            if(Connected &&
               _client is not null &&
               button is not null)
            {
                await _client.SendOnce(button.RemoteName, button.Name, 0);
            }
        }
        catch(Exception ex)
        {
            await ShowExceptionDialog(ex);
        }
    }

    private async Task ShowExceptionDialog(Exception ex)
    {
        var view = ViewResolver.LocateView(this);

        var dialog = new ErrorDialog()
        {
            DataContext = new ErrorDialogViewModel
            {
                Title = "Error",
                Message = ex.Message,
                Details = ex.ToString()
            }
        };

        await DialogHelpers.ShowDialog(view, dialog);
    }
}
