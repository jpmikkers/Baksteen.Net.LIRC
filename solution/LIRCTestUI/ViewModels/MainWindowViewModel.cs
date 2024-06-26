namespace LIRCTestUI.ViewModels;

using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Baksteen.Net.LIRC;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private LIRCClient? _client;

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
            if(ev.Event == LIRCEvent.EventType.ReceivedButton && ev.Button is not null)
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
                        irButton.Seen = true;
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
            }
        }, DispatcherPriority.Background).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await Task.CompletedTask;
    }

    [ObservableProperty]
    private AvaloniaList<IRRemote> _remoteList = new()
    {
        new IRRemote
        {
            Name = "Samsung", ButtonList = new ()
            {
                new IRButton { RemoteName = "Samsung", Name = "KEY_POWER" },
            }
        }
    };

    private bool CanConnect => !Connected;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task Connect()
    {
        try
        {
            RemoteList.Clear();

            _client = new LIRCClient()
            {
                OnLIRCEventAsync = HandleLIRCEvent
            };

            if(UseUnixDomainSocket)
            {
                await _client.Connect(new UnixDomainSocketEndPoint(UnixEndPointAsString));
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

                //await _client.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.220"), 8765));
                await _client.Connect(new IPEndPoint(address, uri.Port));
            }

            foreach(var remote in await _client.ListRemoteControls())
            {
                var irRemote = new IRRemote{ Name = remote };

                foreach(var button in await _client.ListRemoteControlKeys(remote))
                {
                    irRemote.ButtonList.Add(new IRButton { RemoteName = remote, Name = button.Button });
                }

                RemoteList.Add(irRemote);
            }

            Connected = true;
        }
        catch(Exception ex)
        {
            // TODO
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
                button.Seen = false;
            }
        }
    }

    [RelayCommand]
    private async Task DoIt(IRButton? button)
    {
        if(button is not null) await button.TriggerAnimation();

        if(Connected && 
           _client is not null && 
           button is not null)
        {
            await _client.SendOnce(button.RemoteName, button.Name, 0);
        }
    }
}
