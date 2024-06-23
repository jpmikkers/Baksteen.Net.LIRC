namespace Baksteen.Net.LIRC;

public record class ButtonInfo
{
    public string Button { get; init; } = String.Empty;
    public string RemoteControl { get; init; } = String.Empty;
    public byte[] Code { get; init; } = [];
}
