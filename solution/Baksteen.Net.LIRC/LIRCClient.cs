using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
namespace Baksteen.Net.LIRC;

// see https://www.lirc.org/html/lircd.html
//
// to test SIGHUP:
// sudo killall -s SIGHUP lircd

public sealed class LIRCClient : ILIRCClient
{
    private record Response
    {
        public string Command { get; set; } = String.Empty;
        public bool Success { get; set; }
        public List<string> Data { get; set; } = [];
    }

    private const string KWD_BEGIN = "BEGIN";
    private const string KWD_SIGHUP = "SIGHUP";
    private const string KWD_DATA = "DATA";
    private const string KWD_END = "END";
    private const string KWD_SUCCESS = "SUCCESS";
    private const string KWD_ERROR = "ERROR";

    private const string CMD_VERSION = "VERSION";
    private const string CMD_LIST = "LIST";
    private const string CMD_SEND_ONCE = "SEND_ONCE";
    private const string CMD_SEND_START = "SEND_START";
    private const string CMD_SEND_STOP = "SEND_STOP";

    readonly CancellationTokenSource _cts = new();

    NetworkStream? _stream;
    TextWriter _writer = default!;
    TextReader _reader = default!;
    Task? _worker;
    readonly Channel<Response> _responseChannel;
    bool _isConnected;
    readonly ReentrancyPrevention _reentrancyPrevention = new();

    public LIRCClientSettings Settings { get; private init; }

    private LIRCClient(LIRCClientSettings settings)
    {
        this.Settings = settings;
        _responseChannel = Channel.CreateUnbounded<Response>();
    }

    /// <summary>
    /// Connect to LIRC daemon. This can be a local daemon over unix domain socket or via tcp/ip. 
    /// <para>
    /// The default LIRCD unix domain socket endpoint is at <c>/var/run/lirc/lircd</c>.<br/>
    /// The default LIRCD tcp/ip endpoint port number is <c>8765</c>.
    /// </para>
    /// </summary>
    /// <param name="endPoint">endpoint of the LIRC daemon to connect to. This must be either a <see cref="UnixDomainSocketEndPoint"/> 
    /// or a <see cref="IPEndPoint"/>
    /// </param>
    /// <param name="settings">the settings to use during construction of the LIRC client. See also <seealso cref="LIRCClientSettings"/></param>
    /// <returns></returns>
    public static async Task<ILIRCClient> Connect(EndPoint endPoint, LIRCClientSettings settings)
    {
        var result = new LIRCClient(settings);
        await result.Connect(endPoint).ConfigureAwait(false);
        return result;
    }

    private async Task Connect(EndPoint endPoint)
    {
        using var re = _reentrancyPrevention.AssertNotReentrant();
        if(_isConnected) throw new InvalidOperationException("already connected");

        Socket socket;

        if(endPoint.AddressFamily == AddressFamily.Unix)
        {
            socket = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.IP)
            {
                SendTimeout = 10000,
                ReceiveTimeout = Timeout.Infinite,
            };
        }
        else
        {
            socket = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                SendTimeout = 10000,
                ReceiveTimeout = Timeout.Infinite,
            };
        }

        await socket.ConnectAsync(endPoint);

        _stream = new NetworkStream(socket, true);
        _writer = new StreamWriter(_stream, System.Text.Encoding.ASCII, leaveOpen: true);
        _reader = new StreamReader(_stream, System.Text.Encoding.ASCII, leaveOpen: true);
        _worker = ResponseWorker(_cts.Token);

        _isConnected = true;
    }

    private void AssertConnected()
    {
        if(!_isConnected) throw new InvalidOperationException($"{nameof(LIRCClient)} not connected");
    }

    private void FlushResponses()
    {
        while(_responseChannel.Reader.TryRead(out _)) ;
    }

    private async Task<Response> SendReceive(string command)
    {
        FlushResponses();
        await _writer.WriteAsync($"{command}\n").ConfigureAwait(false);
        await _writer.FlushAsync().ConfigureAwait(false);

        using var tcs = new CancellationTokenSource();
        tcs.CancelAfter(Settings.ResponseTimeout);
        var response = await _responseChannel.Reader.ReadAsync(tcs.Token).ConfigureAwait(false);

        if(response.Command != command)
        {
            throw new LIRCException("Invalid response, response command doesn't match request");
        }

        if(!response.Success)
        {
            if(response.Data.Count > 0)
            {
                throw new LIRCException($"Lircd command failed: {response.Data[0]}");
            }
            else
            {
                throw new LIRCException($"Lircd command failed without further details");
            }
        }

        return response;
    }

    public async Task<string> GetVersion()
    {
        AssertConnected();
        using var re = _reentrancyPrevention.AssertNotReentrant();
        var response = await SendReceive(CMD_VERSION).ConfigureAwait(false);
        return response.Data[0];
    }

    public async Task<List<string>> ListRemoteControls()
    {
        AssertConnected();
        using var re = _reentrancyPrevention.AssertNotReentrant();
        var response = await SendReceive(CMD_LIST).ConfigureAwait(false);
        return response.Data;
    }

    public async Task<List<ButtonInfo>> ListRemoteControlKeys(string remoteControl)
    {
        AssertConnected();
        using var re = _reentrancyPrevention.AssertNotReentrant();
        var response = await SendReceive($"{CMD_LIST} {remoteControl}").ConfigureAwait(false);

        List<ButtonInfo> result = [];

        foreach(var line in response.Data)
        {
            var split = line.Split(' ');

            if(split.Length >= 2)
            {
                result.Add(new ButtonInfo
                {
                    Code = Convert.FromHexString(split[0]),
                    Button = split[1],
                    RemoteControl = remoteControl,
                });
            }
        }

        return result;
    }

    public async Task SendOnce(string remoteControl, string button, int repeats = 0)
    {
        AssertConnected();
        using var re = _reentrancyPrevention.AssertNotReentrant();
        var cmd = $"{CMD_SEND_ONCE} {remoteControl} {button}";
        if(repeats > 0) cmd += $" {repeats}";
        _ = await SendReceive(cmd).ConfigureAwait(false);
    }

    public async Task SendStart(string remoteControl, string button)
    {
        AssertConnected();
        using var re = _reentrancyPrevention.AssertNotReentrant();
        _ = await SendReceive($"{CMD_SEND_START} {remoteControl} {button}").ConfigureAwait(false);
    }

    public async Task SendStop(string remoteControl, string button)
    {
        AssertConnected();
        using var re = _reentrancyPrevention.AssertNotReentrant();
        _ = await SendReceive($"{CMD_SEND_STOP} {remoteControl} {button}").ConfigureAwait(false);
    }

    private async Task<String> ReadResponseLine(CancellationToken cancellationToken)
    {
        var result = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        if(result == null)
        {
            throw new LIRCException("connection lost");
        }

        return result;
    }

    private async Task<Response> ReadPacket(string command, CancellationToken cancellationToken)
    {
        var status = await ReadResponseLine(cancellationToken).ConfigureAwait(false);

        if(status != KWD_SUCCESS && status != KWD_ERROR)
        {
            throw new LIRCException("Invalid response format");
        }

        var data_or_end = await ReadResponseLine(cancellationToken).ConfigureAwait(false);

        if(data_or_end == KWD_DATA)
        {
            var length = await ReadResponseLine(cancellationToken).ConfigureAwait(false);

            if(int.TryParse(length, out var numLines))
            {
                // response has x lines of data

                var data = new List<string>(numLines);

                for(int i = 0; i < numLines; i++)
                {
                    var dline = await ReadResponseLine(cancellationToken).ConfigureAwait(false);
                    data.Add(dline);
                }

                var end = await ReadResponseLine(cancellationToken).ConfigureAwait(false);

                if(end == KWD_END)
                {
                    return new Response
                    {
                        Command = command,
                        Success = (status == KWD_SUCCESS),
                        Data = data
                    };
                }
                else
                {
                    throw new LIRCException("Invalid response format");
                }
            }
            else
            {
                throw new LIRCException("Invalid response format");
            }
        }
        else if(data_or_end == KWD_END)
        {
            return new Response
            {
                Command = command,
                Success = (status == KWD_SUCCESS),
                Data = []
            };
        }
        else
        {
            throw new LIRCException("Invalid response format");
        }
    }

    private async Task ResponseWorker(CancellationToken cancellationToken)
    {
        // see https://www.lirc.org/html/lircd.html

        try
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var line = await ReadResponseLine(cancellationToken).ConfigureAwait(false);

                if(line == KWD_BEGIN)
                {
                    // start of response packet or SIGHUP packet
                    var commandorsighup = await ReadResponseLine(cancellationToken).ConfigureAwait(false);

                    if(commandorsighup == KWD_SIGHUP)
                    {
                        if(await ReadResponseLine(cancellationToken).ConfigureAwait(false) != KWD_END)
                        {
                            throw new LIRCException("Invalid sighup message");
                        }

                        await FireEvent(new LIRCEvent
                        {
                            Event = LIRCEvent.EventType.Sighup
                        });
                    }
                    else
                    {
                        var responsePacket = await ReadPacket(commandorsighup, cancellationToken).ConfigureAwait(false);
                        await _responseChannel.Writer.WriteAsync(responsePacket).ConfigureAwait(false);
                    }
                }
                else
                {
                    // decoded button broadcast message
                    // <code> <repeat count> <button name> <remote control name>
                    // e.g., 0000000000f40bf0 00 KEY_UP ANIMAX

                    var split = line.Split(' ');

                    if(split.Length >= 4)
                    {
                        var decodedButton = new DecodedButton
                        {
                            Repeat = Convert.ToInt32(split[1], 16),
                            ButtonInfo = new ButtonInfo
                            {
                                Code = Convert.FromHexString(split[0]),
                                Button = split[2],
                                RemoteControl = split[3]
                            }
                        };

                        //Console.WriteLine($"decoded button: {decodedButton}");
                        await FireEvent(new LIRCEvent
                        {
                            Event = LIRCEvent.EventType.ReceivedButton,
                            Button = decodedButton
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        // Console.WriteLine($"button broadcast format wrong");
                        // TODO: notify? throw? ignore?
                    }
                }
            }
        }
        catch(Exception ex) when(ex is not OperationCanceledException)
        {
            await FireEvent(new LIRCEvent
            {
                Event = LIRCEvent.EventType.Disconnected,
                Reason = ex
            }).ConfigureAwait(false);
        }
    }

    private async Task FireEvent(LIRCEvent ev)
    {
        if(Settings.OnLIRCEventSync is { } syncHandler)
        {
            syncHandler(ev);
        }

        if(Settings.OnLIRCEventAsync is { } asyncHandler)
        {
            await asyncHandler(ev).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task DisposeAsyncCore()
    {
        if(_worker is not null)
        {
            _cts.Cancel();
            await _worker.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            _worker = null;
        }

        _stream?.Dispose();
        _stream = null;
    }
}
