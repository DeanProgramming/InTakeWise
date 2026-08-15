namespace InTakeWise.Services
{
    public sealed class AppClock : IAppClock
    {
        private static readonly TimeZoneInfo LondonTimeZone = ResolveLondonTimeZone();

        private readonly TimeProvider _timeProvider;

        public AppClock(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public DateTime LondonNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, LondonTimeZone);

        public DayOfWeek LondonDayOfWeek => LondonNow.DayOfWeek;

        public (
            DateTime StartUtc,
            DateTime EndUtc
        ) GetTodayLondonRangeUtc()
        {
            var nowLocal = LondonNow;
            var startLocal = nowLocal.Date;
            var endLocal = startLocal.AddDays(1);

            var startUtc =
                TimeZoneInfo.ConvertTimeToUtc(
                    startLocal,
                    LondonTimeZone);

            var endUtc =
                TimeZoneInfo.ConvertTimeToUtc(
                    endLocal,
                    LondonTimeZone);

            return (startUtc, endUtc);
        }

        private static TimeZoneInfo ResolveLondonTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Europe/London");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "GMT Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "GMT Standard Time");
            }
        }
    }
}