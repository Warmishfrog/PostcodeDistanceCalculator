using System.Diagnostics;

namespace PostcodeDistanceCalculator.Services;

/// <summary>
/// Spaces out requests so consecutive calls to <see cref="WaitAsync"/> return at least
/// <paramref name="minInterval"/> apart. The first call returns immediately.
/// </summary>
/// <remarks>
/// Not thread-safe: the app awaits each request before starting the next, so calls never overlap.
/// </remarks>
public sealed class RequestThrottle(TimeSpan minInterval)
{
    private long lastRequestTimestamp;

    /// <summary>Waits until at least the minimum interval has passed since the previous call, then records this call.</summary>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        if (lastRequestTimestamp != 0)
        {
            TimeSpan wait = minInterval - Stopwatch.GetElapsedTime(lastRequestTimestamp);
            if (wait > TimeSpan.Zero) await Task.Delay(wait, cancellationToken);
        }

        lastRequestTimestamp = Stopwatch.GetTimestamp();
    }
}
