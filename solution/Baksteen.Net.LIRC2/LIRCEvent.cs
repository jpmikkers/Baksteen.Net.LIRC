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

    public EventType Event { get; init; }
    public DecodedButton? DecodedButton { get; init; }

    public Exception? Reason { get; init; }
}
