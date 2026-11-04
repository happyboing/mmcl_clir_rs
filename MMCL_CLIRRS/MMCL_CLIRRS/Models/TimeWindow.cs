namespace MMCL_CLIRRS;

public sealed record TimeWindow(TimeSpan Start, TimeSpan End)
{
    public TimeSpan Duration => End - Start;
}
