using InTakeWise.Services;

namespace InTakeWise.Tests.Unit;

public sealed class AppClockTests
{
    [Fact]
    public void LondonNow_UsesGmtDuringWinter()
    {
        var clock = CreateClock(2026, 1, 15, 12);

        Assert.Equal(new DateTime(2026, 1, 15, 12, 0, 0), clock.LondonNow);
    }

    [Fact]
    public void LondonNow_UsesBritishSummerTimeDuringSummer()
    {
        var clock = CreateClock(2026, 7, 15, 12);

        Assert.Equal(new DateTime(2026, 7, 15, 13, 0, 0), clock.LondonNow);
    }

    [Fact]
    public void GetTodayLondonRangeUtc_ReturnsTwentyThreeHoursWhenClocksGoForward()
    {
        var clock = CreateClock(2026, 3, 29, 12);

        var (startUtc, endUtc) = clock.GetTodayLondonRangeUtc();

        Assert.Equal(new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc), startUtc);
        Assert.Equal(new DateTime(2026, 3, 29, 23, 0, 0, DateTimeKind.Utc), endUtc);
        Assert.Equal(TimeSpan.FromHours(23), endUtc - startUtc);
    }

    [Fact]
    public void GetTodayLondonRangeUtc_ReturnsTwentyFiveHoursWhenClocksGoBack()
    {
        var clock = CreateClock(2026, 10, 25, 12);

        var (startUtc, endUtc) = clock.GetTodayLondonRangeUtc();

        Assert.Equal(new DateTime(2026, 10, 24, 23, 0, 0, DateTimeKind.Utc), startUtc);
        Assert.Equal(new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), endUtc);
        Assert.Equal(TimeSpan.FromHours(25), endUtc - startUtc);
    }

    private static AppClock CreateClock(int year, int month, int day, int hour) =>
        new(new FixedTimeProvider(new DateTimeOffset(year, month, day, hour, 0, 0, TimeSpan.Zero)));

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
