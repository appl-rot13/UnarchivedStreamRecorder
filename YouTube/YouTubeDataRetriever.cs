
namespace UnarchivedStreamRecorder.YouTube;

using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnarchivedStreamRecorder.Utilities;

public class YouTubeDataRetriever(IHttpResponseReader responseReader, string apiKey)
{
    public static string GetFeedUrl(string channelId)
    {
        return $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";
    }

    public static string GetVideoUrl(string videoId)
    {
        // 参考: https://developers.google.com/youtube/player_parameters
        // autoplay=1 -> 動画を自動再生する
        // controls=0 -> シークバーを表示しない
        //      rel=0 -> 動画終了時に関連動画を表示しない

        return $"https://www.youtube.com/embed/{videoId}?autoplay=1&controls=0&rel=0";
    }

    public string GetVideoDetailsUrl(string videoId, string part, string? fields = null)
    {
        // 参考: https://developers.google.com/youtube/v3/getting-started
        //       https://developers.google.com/youtube/v3/docs/videos/list
        // key    -> APIキー
        // id     -> 動画ID
        // part   -> レスポンスとして欲しい情報
        // fields -> レスポンスの絞り込み(帯域幅使用量の削減)

        var query = $"key={apiKey}&id={videoId}&part={part}";
        if (!string.IsNullOrEmpty(fields))
        {
            query += $"&fields={fields}";
        }

        return $"https://www.googleapis.com/youtube/v3/videos?{query}";
    }

    public IEnumerable<((string Id, string Name) Channel, string Id, string Title)> EnumerateLatestVideos(string channelId)
    {
        var url = GetFeedUrl(channelId);
        var feed = XElement.Load(url);

        var xmlNamespace = feed.GetDefaultNamespace();
        var youtubeNamespace = feed.GetNamespaceOfPrefix("yt") ?? XNamespace.None;

        var channelName = feed.Element(xmlNamespace.GetName("title"))?.Value;
        if (string.IsNullOrWhiteSpace(channelName))
        {
            yield break;
        }

        var channel = (channelId, channelName);
        foreach (var entry in feed.Elements(xmlNamespace.GetName("entry")))
        {
            var videoId = entry.Element(youtubeNamespace.GetName("videoId"))?.Value;
            if (string.IsNullOrWhiteSpace(videoId))
            {
                continue;
            }

            var videoTitle = entry.Element(xmlNamespace.GetName("title"))?.Value;
            if (string.IsNullOrWhiteSpace(videoTitle))
            {
                continue;
            }

            yield return (channel, videoId, videoTitle);
        }
    }

    public async Task<JToken?> GetLiveStreamingDetailsAsync(string videoId)
    {
        const string partString = "liveStreamingDetails";
        var response = await this.GetVideoDetailsAsync(videoId, partString, $"items({partString})");

        var items = response?["items"];
        var item = items?.First;
        if (response != null && items != null && item == null)
        {
            // 配信が削除された場合
            return null;
        }

        var liveStreamingDetails = item?[partString];
        if (liveStreamingDetails == null)
        {
            throw new InvalidOperationException($"Unexpected response: {response}");
        }

        return liveStreamingDetails;
    }

    public async Task<JObject?> GetVideoDetailsAsync(string videoId, string part, string? fields = null)
    {
        var url = this.GetVideoDetailsUrl(videoId, part, fields);
        var response = await responseReader.GetResponseAsync(url);

        // 日時のデシリアライズでタイムゾーン情報が失われる問題の対策のため、以下の変換とする
        var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
        return JsonConvert.DeserializeObject<JObject>(response, settings);
    }
}
