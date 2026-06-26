namespace ISO11820.Core.Contracts;

public interface IRuntimeClock
{
    DateTime Now { get; }
}
