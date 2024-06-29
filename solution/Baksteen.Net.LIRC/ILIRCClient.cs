namespace Baksteen.Net.LIRC;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public record LIRCClientSettings
{
    public Func<LIRCEvent, Task>? OnLIRCEventAsync { get; set; }
    public Action<LIRCEvent>? OnLIRCEventSync { get; set; }
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(10);
}

public interface ILIRCClient : IAsyncDisposable, IDisposable
{
    LIRCClientSettings Settings { get; }

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
