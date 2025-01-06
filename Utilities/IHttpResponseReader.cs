
namespace UnarchivedStreamRecorder.Utilities;

public interface IHttpResponseReader
{
    public Task<string> GetResponseAsync(string url);
}
