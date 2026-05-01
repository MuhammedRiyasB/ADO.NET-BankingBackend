namespace Banking.Application.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}
