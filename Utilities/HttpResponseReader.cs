
namespace UnarchivedStreamRecorder.Utilities;

public class HttpResponseReader(HttpClient client) : IHttpResponseReader
{
    public async Task<string> GetResponseAsync(string url)
    {
        var response = await client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}
