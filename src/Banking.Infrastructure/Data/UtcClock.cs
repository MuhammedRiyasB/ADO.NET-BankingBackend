using Banking.Application.Interfaces;

namespace Banking.Infrastructure.Data;

internal sealed class UtcClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
