using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
namespace Baksteen.Net.LIRC;

// see https://www.lirc.org/html/lircd.html
//
// to test SIGHUP:
// sudo killall -s SIGHUP lircd

public sealed class LIRCClient : IAsyncDisposable, IDisposable
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

    public Func<LIRCEvent, Task> OnEvent { get; set; } = _ => Task.CompletedTask;
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public LIRCClient()
    {
        _responseChannel = Channel.CreateUnbounded<Response>();
    }

    public async Task Connect(UnixDomainSocketEndPoint unixDomainSocketEndPoint)
    {
        if(_isConnected) throw new InvalidOperationException("already connected");
        var socket = new Socket(unixDomainSocketEndPoint.AddressFamily, SocketType.Stream, ProtocolType.IP)
        {
            SendTimeout = 10000,
            ReceiveTimeout = Timeout.Infinite
        };
        await socket.ConnectAsync(unixDomainSocketEndPoint);
        CompleteConnection(socket);
    }

    public async Task Connect(IPEndPoint ipEndPoint)
    {
        if(_isConnected) throw new InvalidOperationException("already connected");
        var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
            SendTimeout = 10000,
            ReceiveTimeout = Timeout.Infinite
        };
        await socket.ConnectAsync(ipEndPoint);
        CompleteConnection(socket);
    }

    private void CompleteConnection(Socket socket)
    {
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
        await _writer.WriteAsync($"{command}\n");
        await _writer.FlushAsync();

        using var tcs = new CancellationTokenSource();
        tcs.CancelAfter(ResponseTimeout);
        var response = await _responseChannel.Reader.ReadAsync(tcs.Token);

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
        var response = await SendReceive(CMD_VERSION);
        return response.Data[0];
    }

    public async Task<List<string>> ListRemoteControls()
    {
        AssertConnected();
        var response = await SendReceive(CMD_LIST);
        return response.Data;
    }

    public async Task<List<ButtonInfo>> ListRemoteControlKeys(string remoteControl)
    {
        AssertConnected();
        var response = await SendReceive($"{CMD_LIST} {remoteControl}");

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
        var cmd = $"{CMD_SEND_ONCE} {remoteControl} {button}";
        if(repeats > 0) cmd += $" {repeats}";
        _ = await SendReceive(cmd);
    }

    public async Task SendStart(string remoteControl, string button)
    {
        AssertConnected();
        _ = await SendReceive($"{CMD_SEND_START} {remoteControl} {button}");
    }

    public async Task SendStop(string remoteControl, string button)
    {
        AssertConnected();
        _ = await SendReceive($"{CMD_SEND_STOP} {remoteControl} {button}");
    }

    private async Task<String> ReadResponseLine(CancellationToken cancellationToken)
    {
        var result = await _reader.ReadLineAsync(cancellationToken);

        if(result == null)
        {
            throw new LIRCException("connection lost");
        }

        return result;
    }

    private async Task<Response> ReadPacket(string command, CancellationToken cancellationToken)
    {
        var status = await ReadResponseLine(cancellationToken);

        if(status != KWD_SUCCESS && status != KWD_ERROR)
        {
            throw new LIRCException("Invalid response format");
        }

        var data_or_end = await ReadResponseLine(cancellationToken);

        if(data_or_end == KWD_DATA)
        {
            var length = await ReadResponseLine(cancellationToken);

            if(int.TryParse(length, out var numLines))
            {
                // response has x lines of data

                var data = new List<string>(numLines);

                for(int i = 0; i < numLines; i++)
                {
                    var dline = await ReadResponseLine(cancellationToken);
                    data.Add(dline);
                }

                var end = await ReadResponseLine(cancellationToken);

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
                var line = await ReadResponseLine(cancellationToken);

                if(line == KWD_BEGIN)
                {
                    // start of response packet or SIGHUP packet
                    var commandorsighup = await ReadResponseLine(cancellationToken);

                    if(commandorsighup == KWD_SIGHUP)
                    {
                        if(await ReadResponseLine(cancellationToken) != KWD_END)
                        {
                            throw new LIRCException("Invalid sighup message");
                        }

                        await OnEvent(new LIRCEvent
                        {
                            Event = LIRCEvent.EventType.Sighup
                        });
                    }
                    else
                    {
                        var responsePacket = await ReadPacket(commandorsighup, cancellationToken);
                        await _responseChannel.Writer.WriteAsync(responsePacket);
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
                        await OnEvent(new LIRCEvent
                        {
                            Event = LIRCEvent.EventType.ReceivedButton,
                            DecodedButton = decodedButton
                        });
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
            await OnEvent(new LIRCEvent
            {
                Event = LIRCEvent.EventType.Disconnected,
                Reason = ex
            });
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
