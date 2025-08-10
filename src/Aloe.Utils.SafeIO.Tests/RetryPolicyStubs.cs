using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aloe.Utils.SafeIO.Tests;

internal sealed class FixedRetryPolicy : ISafeRetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;

    public FixedRetryPolicy(int maxRetries, TimeSpan delay)
    {
        this._maxRetries = maxRetries;
        this._delay = delay;
    }

    public bool Execute(Func<bool> attempt)
    {
        for (var i = 0; i <= this._maxRetries; i++)
        {
            if (attempt())
            {
                return true;
            }

            Thread.Sleep(this._delay);
        }

        return false;
    }

    public async Task<bool> ExecuteAsync(Func<CancellationToken, Task<bool>> attempt, CancellationToken ct)
    {
        for (var i = 0; i <= this._maxRetries; i++)
        {
            ct.ThrowIfCancellationRequested();

            if (await attempt(ct))
            {
                return true;
            }

            await Task.Delay(this._delay, ct);
        }

        return false;
    }
}



