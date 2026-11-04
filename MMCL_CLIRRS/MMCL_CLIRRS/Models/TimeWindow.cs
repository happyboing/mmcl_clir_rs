namespace MMCL_CLIRRS;

// A stretch of time within one day, for example 14:00 to 16:00.
public sealed record TimeWindow(TimeSpan Start, TimeSpan End)
{
    public TimeSpan Duration => End - Start;
}
