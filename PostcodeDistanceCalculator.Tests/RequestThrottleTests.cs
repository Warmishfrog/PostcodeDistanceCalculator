using System.Diagnostics;
using PostcodeDistanceCalculator.Services;

namespace PostcodeDistanceCalculator.Tests;

public class RequestThrottleTests
{
    [Fact]
    public async Task First_call_is_immediate_and_later_calls_are_spaced_out()
    {
        var interval = TimeSpan.FromMilliseconds(200);
        var throttle = new RequestThrottle(interval);
        var clock = Stopwatch.StartNew();

        await throttle.WaitAsync();
        var first = clock.Elapsed;
        await throttle.WaitAsync();
        await throttle.WaitAsync();
        var third = clock.Elapsed;

        Assert.True(first < TimeSpan.FromMilliseconds(100), $"first call took {first.TotalMilliseconds} ms");
        // Task.Delay can wake a few ms early on Windows' timer resolution, so allow a small margin
        Assert.True(third >= 2 * interval - TimeSpan.FromMilliseconds(30), $"three calls took only {third.TotalMilliseconds} ms");
    }

    [Fact]
    public async Task No_wait_when_the_interval_has_already_passed()
    {
        var throttle = new RequestThrottle(TimeSpan.FromMilliseconds(50));
        await throttle.WaitAsync();
        await Task.Delay(100);

        var clock = Stopwatch.StartNew();
        await throttle.WaitAsync();

        Assert.True(clock.Elapsed < TimeSpan.FromMilliseconds(40), $"waited {clock.Elapsed.TotalMilliseconds} ms");
    }
}
