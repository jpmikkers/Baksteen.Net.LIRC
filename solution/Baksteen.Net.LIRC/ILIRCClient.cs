namespace Baksteen.Net.LIRC;

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

public interface ILIRCClient : IAsyncDisposable, IDisposable
{
    Func<LIRCEvent, Task>? OnLIRCEventAsync { get; set; }
    Action<LIRCEvent>? OnLIRCEventSync { get; set; }
    TimeSpan ResponseTimeout { get; set; }

    Task Connect(EndPoint endPoint);
    Task<string> GetVersion();
    Task<List<ButtonInfo>> ListRemoteControlKeys(string remoteControl);
    Task<List<string>> ListRemoteControls();
    Task SendOnce(string remoteControl, string button, int repeats = 0);
    Task SendStart(string remoteControl, string button);
    Task SendStop(string remoteControl, string button);
}