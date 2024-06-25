namespace Baksteen.Net.LIRC;

public record LIRCEvent
{
    public enum EventType
    {
        /// <summary>
        /// Occurs when an infrared remote control button press was received
        /// </summary>
        ReceivedButton,

        /// <summary>
        /// Occurs when the connection to the LIRC daemon was lost
        /// </summary>
        Disconnected,

        /// <summary>
        /// Occurs when the LIRC daemon received a SIGHUP signal, which means it re-read its remotecontrol configurations.
        /// </summary>
        Sighup
    }

    /// <summary>
    /// Which type of LIRC event occurred
    /// </summary>
    public EventType Event { get; init; }

    /// <summary>
    /// IR button command received, only has a value when <see cref="Event"/> is set to <see cref="EventType.ReceivedButton"/>
    /// </summary>
    public DecodedButton? Button { get; init; }

    /// <summary>
    /// Contains the disconnect failure reason, only has a value when <see cref="Event"/> is set to <see cref="EventType.Disconnected"/>
    /// </summary>
    public Exception? Reason { get; init; }
}
