using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace InTakeWise.Services;

public interface IAiRequestGate
{
    ValueTask EnsureAllowedAsync(string userId, AiOperation operation, CancellationToken cancellationToken = default);
}

public sealed class AiRequestGate : IAiRequestGate
{
    private static readonly TimeSpan DailyWindow = TimeSpan.FromDays(1);

    private readonly ConcurrentDictionary<LimitKey, WindowCounter> _burstCounters = new();
    private readonly ConcurrentDictionary<LimitKey, WindowCounter> _dailyCounters = new();
    private readonly AiSafetyOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AiRequestGate> _logger;
    private int _requestsSinceCleanup;

    public AiRequestGate(IOptions<AiSafetyOptions> options, TimeProvider timeProvider, ILogger<AiRequestGate> logger)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public ValueTask EnsureAllowedAsync(string userId, AiOperation operation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("A valid user is required to use AI generation.");
        }

        var now = _timeProvider.GetUtcNow();
        var limits = _options.GetLimits(operation);
        var key = new LimitKey(userId, operation);

        if (!TryConsume(_burstCounters, key, now, TimeSpan.FromSeconds(limits.BurstWindowSeconds), limits.BurstPermitLimit, out var burstRetryAfter))
        {
            LogRejection(userId, operation, dailyQuotaExceeded: false);

            throw new AiRequestLimitException($"Too many {operation.GetDisplayName()} requests. Please wait a moment and try again.", burstRetryAfter, dailyQuotaExceeded: false);
        }

        if (!TryConsume(_dailyCounters,  key, now, DailyWindow, limits.DailyPermitLimit,  out var quotaRetryAfter))
        {
            LogRejection(userId, operation, dailyQuotaExceeded: true);

            throw new AiRequestLimitException($"The 24-hour {operation.GetDisplayName()} limit has been reached. Please try again later.", quotaRetryAfter, dailyQuotaExceeded: true);
        }

        if (Interlocked.Increment(ref _requestsSinceCleanup) >= 256)
        {
            Interlocked.Exchange(ref _requestsSinceCleanup, 0);
            RemoveExpiredCounters(_burstCounters, now); 
            RemoveExpiredCounters(_dailyCounters, now);
        }

        return ValueTask.CompletedTask;
    }

    private void LogRejection(string userId, AiOperation operation, bool dailyQuotaExceeded)
    {
        _logger.LogWarning("AI request rejected for user {UserReference}. Operation={Operation}; DailyQuotaExceeded={DailyQuotaExceeded}.", AiLogSanitizer.UserReference(userId), operation, dailyQuotaExceeded);
    }

    private static bool TryConsume(ConcurrentDictionary<LimitKey, WindowCounter> counters, LimitKey key, DateTimeOffset now, TimeSpan window, int permitLimit, out TimeSpan retryAfter)
    {
        var counter = counters.GetOrAdd(
            key,
            _ => new WindowCounter(now.Add(window)));

        lock (counter.SyncRoot)
        {
            if (now >= counter.WindowEndsAt)
            {
                counter.WindowEndsAt = now.Add(window);
                counter.Count = 0;
            }

            if (counter.Count >= permitLimit)
            {
                retryAfter = counter.WindowEndsAt - now;
                return false;
            }

            counter.Count++;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private static void RemoveExpiredCounters(ConcurrentDictionary<LimitKey, WindowCounter> counters, DateTimeOffset now)
    {
        foreach (var pair in counters)
        {
            lock (pair.Value.SyncRoot)
            {
                if (now >= pair.Value.WindowEndsAt)
                {
                    counters.TryRemove(pair.Key, out _);
                }
            }
        }
    }

    private readonly record struct LimitKey(string UserId, AiOperation Operation);

    private sealed class WindowCounter
    {
        public WindowCounter(DateTimeOffset windowEndsAt)
        {
            WindowEndsAt = windowEndsAt;
        }

        public object SyncRoot { get; } = new();
        public DateTimeOffset WindowEndsAt { get; set; }
        public int Count { get; set; }
    }
}