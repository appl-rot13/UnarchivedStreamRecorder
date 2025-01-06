
namespace UnarchivedStreamRecorder.Utilities;

public interface ILiveStream
{
    public string Url { get; }

    public Task<bool> WaitForStart();

    public Task<bool> WaitForEnd();
}
