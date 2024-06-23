namespace Baksteen.Net.LIRC;

public class LIRCException : Exception
{
    public LIRCException() { }
    public LIRCException(string message) : base(message) { }
    public LIRCException(string message, Exception inner) : base(message, inner) { }
}
