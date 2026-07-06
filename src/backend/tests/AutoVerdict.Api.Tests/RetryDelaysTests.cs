using AutoVerdict.ProcessingService.Consumers;

namespace AutoVerdict.Api.Tests;

public sealed class RetryDelaysTests
{
    [Theory]
    [InlineData(1ul, 1)]
    [InlineData(2ul, 4)]
    [InlineData(3ul, 16)]
    [InlineData(4ul, 30)]
    [InlineData(9ul, 30)]  // clamped
    [InlineData(0ul, 1)]   // defensive: metadata missing
    public void ForDelivery_FollowsSchedule(ulong delivered, int expectedMinutes) =>
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), RetryDelays.ForDelivery(delivered));
}
