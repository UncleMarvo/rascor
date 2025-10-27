using Rascor.Domain;
using Rascor.Domain.Entities;

namespace Rascor.Infrastructure;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
