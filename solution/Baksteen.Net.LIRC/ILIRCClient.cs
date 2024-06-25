namespace Baksteen.Net.LIRC;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public interface ILIRCClient : IAsyncDisposable, IDisposable
{
    Func<LIRCEvent, Task>? OnLIRCEventAsync { get; set; }
    Action<LIRCEvent>? OnLIRCEventSync { get; set; }
    TimeSpan ResponseTimeout { get; set; }

    /// <summary>
    /// Connect to LIRC daemon. This can be a local daemon over unix domain socket or via tcp/ip. 
    /// <para>
    /// The default LIRCD unix domain socket endpoint is at <c>/var/run/lirc/lircd</c>.<br/>
    /// The default LIRCD tcp/ip endpoint port number is <c>8765</c>.
    /// </para>
    /// </summary>
    /// <param name="endPoint">endpoint of the LIRC daemon to connect to. This must be either a <see cref="UnixDomainSocketEndPoint"/> 
    /// or a <see cref="IPEndPoint"/></param>
    /// <returns></returns>
    Task Connect(EndPoint endPoint);

    /// <summary>
    /// Retrieve LIRC daemon version string.
    /// </summary>
    /// <returns>LIRC daemon version string</returns>
    Task<string> GetVersion();

    /// <summary>
    /// List all names of remote controls that are configured on the LIRC daemon.
    /// </summary>
    /// <returns>list of configured remote controls</returns>
    Task<List<string>> ListRemoteControls();

    /// <summary>
    /// List the names of the buttons for the given remote control.
    /// </summary>
    /// <param name="remoteControl">name of the remote control to retrieve the buttons from</param>
    /// <returns>list of names of all the known buttons</returns>
    Task<List<ButtonInfo>> ListRemoteControlKeys(string remoteControl);

    /// <summary>
    /// Send a IR remote control command with the specified number of repeats.
    /// </summary>
    /// <param name="remoteControl">name of the remote control</param>
    /// <param name="button">name of the button</param>
    /// <param name="repeats">how often to repeat the command after the initial send</param>
    /// <returns></returns>
    Task SendOnce(string remoteControl, string button, int repeats = 0);

    /// <summary>
    /// Start repeatedly sending a IR remote control command
    /// </summary>
    /// <param name="remoteControl">name of the remote control</param>
    /// <param name="button">name of the button</param>
    /// <returns></returns>
    Task SendStart(string remoteControl, string button);

    /// <summary>
    /// Stop a repeat send that was initiated using <see cref="SendStart(string, string)"/>
    /// </summary>
    /// <param name="remoteControl">name of the remote control</param>
    /// <param name="button">name of the button</param>
    /// <returns></returns>
    Task SendStop(string remoteControl, string button);
}
