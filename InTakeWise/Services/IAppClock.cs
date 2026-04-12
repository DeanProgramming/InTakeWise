namespace InTakeWise.Services
{
    public interface IAppClock
    {
        DateTime UtcNow { get; }
        DateTime LondonNow { get; }
        DayOfWeek LondonDayOfWeek { get; }
        (DateTime StartUtc, DateTime EndUtc) GetTodayLondonRangeUtc();
    } 
}