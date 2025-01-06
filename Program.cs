
using System.Configuration;

using OBSWebsocketDotNet;

using UnarchivedStreamRecorder.OBS;
using UnarchivedStreamRecorder.Utilities;
using UnarchivedStreamRecorder.Utilities.Extensions;
using UnarchivedStreamRecorder.Utilities.Logging;
using UnarchivedStreamRecorder.YouTube;

var logger = Logger.GetInstance();

try
{
    var youtube = new YouTubeDataRetriever(
        new HttpResponseReader(new HttpClient()),
        ReadAppSettings("YouTube.APIKey"));

    var recorder = new OBSRecorder(
        logger,
        new OBSLauncher(logger, ReadAppSettings("OBS.FilePath")),
        new OBSWebsocket(),
        ReadAppSettings("OBS.URL"),
        ReadAppSettings("OBS.Password", string.Empty),
        ReadAppSettings("OBS.SceneName"),
        ReadAppSettings("OBS.SourceName"));

    var startBufferTime = TimeSpan.FromMinutes(double.Parse(ReadAppSettings("StartBufferMinutes")));
    var skipEndCheckTime = TimeSpan.FromMinutes(double.Parse(ReadAppSettings("SkipEndCheckMinutes")));
    var checkPollingTime = TimeSpan.FromMinutes(double.Parse(ReadAppSettings("CheckPollingMinutes"))).MinLimit(TimeSpan.FromSeconds(1));

    var channelIds = ReadAppSettings("ChannelIDs").Split(',').Select(s => s.Trim()).ToArray();
    var keywords = ReadAppSettings("Keywords").Split(',').Select(s => s.Trim()).ToArray();

    foreach (var video in channelIds.SelectMany(youtube.EnumerateLatestVideos))
    {
        if (!keywords.Any(keyword => video.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            continue;
        }

        logger.WriteLine(
            $"A video targeted for recording has been found.\n"
            + $"  Channel ID:   {video.Channel.Id}\n"
            + $"  Channel Name: {video.Channel.Name}\n"
            + $"  Video ID:     {video.Id}\n"
            + $"  Video Title:  {video.Title}\n");

        await recorder.RecordAsync(
            new YouTubeLiveStream(
                logger,
                youtube,
                video.Id,
                startBufferTime,
                skipEndCheckTime,
                checkPollingTime));

        logger.WriteLine();
    }
}
catch (Exception e)
{
    logger.WriteLine($"{e}");
    Console.ReadLine();
}

return;

string ReadAppSettings(string key, string? defaultValue = null)
{
    var value = ConfigurationManager.AppSettings[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        return defaultValue ?? throw new ConfigurationErrorsException($"The given key '{key}' was not present in the application settings.");
    }

    return value;
}
