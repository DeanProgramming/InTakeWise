using InTakeWise.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InTakeWise.Tests.Unit;

public sealed class AiRequestGateTests
{
    [Fact]
    public async Task EnsureAllowedAsync_RejectsRequestAboveBurstLimit()
    {
        var gate = CreateGate(burstLimit: 2, burstWindowSeconds: 60);

        await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis);
        await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis);

        var exception = await Assert.ThrowsAsync<AiRequestLimitException>(async () =>
            await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis));

        Assert.False(exception.DailyQuotaExceeded);
        Assert.Equal(TimeSpan.FromSeconds(60), exception.RetryAfter);
    }

    [Fact]
    public async Task EnsureAllowedAsync_EnforcesAndResetsRollingDailyQuota()
    {
        var time = new MutableTimeProvider();
        var gate = CreateGate(time, burstLimit: 10, dailyLimit: 2);

        await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis);
        await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis);

        var exception = await Assert.ThrowsAsync<AiRequestLimitException>(async () =>
            await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis));

        Assert.True(exception.DailyQuotaExceeded);
        Assert.Equal(TimeSpan.FromDays(1), exception.RetryAfter);

        time.Advance(TimeSpan.FromDays(1));
        await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis);
    }

    [Fact]
    public async Task EnsureAllowedAsync_IsolatesCountersByUserAndOperation()
    {
        var gate = CreateGate(burstLimit: 1);

        await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis);
        await gate.EnsureAllowedAsync("user-b", AiOperation.MealAnalysis);
        await gate.EnsureAllowedAsync("user-a", AiOperation.WorkoutAnalysis);

        await Assert.ThrowsAsync<AiRequestLimitException>(async () =>
            await gate.EnsureAllowedAsync("user-a", AiOperation.MealAnalysis));
    }

    private static AiRequestGate CreateGate(
        MutableTimeProvider? timeProvider = null,
        int burstLimit = 4,
        int burstWindowSeconds = 60,
        int dailyLimit = 25)
    {
        var limit = new AiOperationLimitOptions
        {
            BurstPermitLimit = burstLimit,
            BurstWindowSeconds = burstWindowSeconds,
            DailyPermitLimit = dailyLimit
        };

        var options = new AiSafetyOptions
        {
            MealAnalysis = Clone(limit),
            WorkoutAnalysis = Clone(limit),
            ShoppingPlan = Clone(limit),
            ReceiptAnalysis = Clone(limit)
        };

        return new AiRequestGate(
            Options.Create(options),
            timeProvider ?? new MutableTimeProvider(),
            NullLogger<AiRequestGate>.Instance);
    }

    private static AiOperationLimitOptions Clone(AiOperationLimitOptions source) => new()
    {
        BurstPermitLimit = source.BurstPermitLimit,
        BurstWindowSeconds = source.BurstWindowSeconds,
        DailyPermitLimit = source.DailyPermitLimit
    };

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow =
            new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);
    }
}
