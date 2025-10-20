using SiteAttendance.Domain;

namespace SiteAttendance.Infrastructure;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
