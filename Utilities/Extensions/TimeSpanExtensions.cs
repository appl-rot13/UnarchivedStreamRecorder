
namespace UnarchivedStreamRecorder.Utilities.Extensions;

public static class TimeSpanExtensions
{
    public static TimeSpan MinLimit(this TimeSpan timeSpan, TimeSpan min) =>
        timeSpan < min ? min : timeSpan;

    public static TimeSpan MaxLimit(this TimeSpan timeSpan, TimeSpan max) =>
        timeSpan > max ? max : timeSpan;
}
