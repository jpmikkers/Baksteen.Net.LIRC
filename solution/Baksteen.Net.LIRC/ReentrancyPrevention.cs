namespace Baksteen.Net.LIRC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class ReentrancyPrevention
{
    protected int _counter;

    private class DisposeToken : IDisposable
    {
        private ReentrancyPrevention _parent;

        public DisposeToken(ReentrancyPrevention parent)
        {
            _parent = parent;
            if(Interlocked.Increment(ref _parent._counter) > 1)
            {
                // throwing in the constructor so the caller isn't going
                // to get a token to decrement the counter, so we have to do that now.
                Interlocked.Decrement(ref _parent._counter);
                throw new InvalidOperationException("This method is not reentrant");
            }
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref _parent._counter);
        }
    }

    public IDisposable AssertNotReentrant()
    {
        return new DisposeToken(this);
    }
}
