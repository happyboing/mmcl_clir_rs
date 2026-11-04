namespace MMCL_CLIRRS;

public class StaffAccessControl
{
    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);

    private readonly string _pin;
    private int _failedAttempts;
    private DateTime _lockedUntil = DateTime.MinValue;

    public StaffAccessControl(string pin)
    {
        _pin = pin;
    }

    public bool IsLockedOut(out int secondsLeft)
    {
        TimeSpan remaining = _lockedUntil - DateTime.Now;
        secondsLeft = (int)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
        return remaining > TimeSpan.Zero;
    }

    public bool TryUnlock(string enteredPin)
    {
        if (IsLockedOut(out _)) return false;

        if (enteredPin == _pin)
        {
            _failedAttempts = 0;
            return true;
        }

        _failedAttempts++;
        if (_failedAttempts >= MaxFailedAttempts)
        {
            _lockedUntil = DateTime.Now + LockoutDuration;
            _failedAttempts = 0;
        }
        return false;
    }
}