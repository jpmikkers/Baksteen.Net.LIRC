namespace Baksteen.Net.LIRC;

public record class DecodedButton
{
    public ButtonInfo ButtonInfo { get; init; } = new();
    public int Repeat { get; init; }
}
