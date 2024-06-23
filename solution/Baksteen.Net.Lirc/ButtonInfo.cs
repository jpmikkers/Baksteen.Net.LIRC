namespace Baksteen.Net.LIRC;

public record class ButtonInfo
{
    public byte[] Code { get; set; } = [];
    public string Button { get; set; } = String.Empty;
    public string RemoteControl { get; set; } = String.Empty;
}
