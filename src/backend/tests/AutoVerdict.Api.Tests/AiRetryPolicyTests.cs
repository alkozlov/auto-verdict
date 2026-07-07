using AutoVerdict.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace AutoVerdict.Api.Tests;

public sealed class AiRetryPolicyTests
{
    private static AiRetryPolicy Create(FakeTimeProvider clock) =>
        new(clock, NullLogger<AiRetryPolicy>.Instance);

    private static async Task<T> RunWithClock<T>(FakeTimeProvider clock, Task<T> task)
    {
        while (!task.IsCompleted)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            await Task.Yield();
        }
        return await task;
    }

    [Fact]
    public async Task Retries_TransientFailures_ThenSucceeds()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var task = Create(clock).ExecuteAsync(_ =>
        {
            calls++;
            if (calls < 3) throw new HttpRequestException("boom");
            return Task.FromResult(42);
        }, CancellationToken.None);

        Assert.Equal(42, await RunWithClock(clock, task));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task GivesUp_AfterThreeAttempts()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var task = Create(clock).ExecuteAsync<int>(_ =>
        {
            calls++;
            throw new HttpRequestException("always");
        }, CancellationToken.None);

        await Assert.ThrowsAsync<HttpRequestException>(() => RunWithClock(clock, task));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task DoesNotRetry_NonTransientErrors()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var task = Create(clock).ExecuteAsync<int>(_ =>
        {
            calls++;
            throw new InvalidOperationException("client bug");
        }, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DoesNotRetry_WhenCallerCancelled()
    {
        var clock = new FakeTimeProvider();
        using var cts = new CancellationTokenSource();
        var calls = 0;
        var task = Create(clock).ExecuteAsync<int>(_ =>
        {
            calls++;
            cts.Cancel();
            throw new TaskCanceledException();
        }, cts.Token);

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Timeout_WithLiveCallerToken_IsRetried()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        var task = Create(clock).ExecuteAsync(_ =>
        {
            calls++;
            if (calls == 1) throw new TaskCanceledException(); // HttpClient timeout shape
            return Task.FromResult(1);
        }, CancellationToken.None);

        Assert.Equal(1, await RunWithClock(clock, task));
        Assert.Equal(2, calls);
    }
}
