namespace Baksteen.Net.LIRC;

public record class DecodedButton
{
    public ButtonInfo ButtonInfo { get; set; } = new();
    public int Repeat { get; set; }
}
