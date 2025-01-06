
namespace UnarchivedStreamRecorder.YouTube;

using Newtonsoft.Json.Linq;

using UnarchivedStreamRecorder.Utilities;
using UnarchivedStreamRecorder.Utilities.Logging;

public class YouTubeLiveStream(
    ILogger? logger,
    YouTubeDataRetriever youtube,
    string videoId,
    TimeSpan startBufferTime,
    TimeSpan skipEndCheckTime,
    TimeSpan checkPollingTime) : ILiveStream
{
    public string Url => YouTubeDataRetriever.GetVideoUrl(videoId);

    public async Task<bool> WaitForStart()
    {
        while (true)
        {
            var liveStreamingDetails = await youtube.GetLiveStreamingDetailsAsync(videoId);
            if (liveStreamingDetails == null)
            {
                // 配信が削除された場合
                logger?.WriteLine("The video is either private or has been removed.");
                return false;
            }

            var actualStartTime = liveStreamingDetails["actualStartTime"]?.Value<string>();
            if (!string.IsNullOrEmpty(actualStartTime))
            {
                // 配信予定時刻より早く配信が開始した場合
                logger?.WriteLine("The video has started.");
                return true;
            }

            var scheduledStartTime = liveStreamingDetails["scheduledStartTime"]?.Value<string>();
            if (string.IsNullOrEmpty(scheduledStartTime))
            {
                throw new InvalidOperationException($"Unexpected object: {liveStreamingDetails}");
            }

            var scheduledStartLocalTime = DateTime.Parse(scheduledStartTime).ToLocalTime();
            logger?.WriteLine($"The video is scheduled to start at {scheduledStartLocalTime}.");

            var bufferingStartLocalTime = scheduledStartLocalTime.Subtract(startBufferTime);
            var timeRemaining = bufferingStartLocalTime - DateTime.Now;
            if (timeRemaining <= TimeSpan.Zero)
            {
                // 配信予定時刻の場合
                logger?.WriteLine("Start buffering.");
                return true;
            }

            logger?.WriteLine($"Wait until {bufferingStartLocalTime} (Time remaining: {timeRemaining}).");
            await Task.Delay(timeRemaining);
        }
    }

    public async Task<bool> WaitForEnd()
    {
        if (skipEndCheckTime > TimeSpan.Zero)
        {
            logger?.WriteLine($"Skip the video end check for {skipEndCheckTime.TotalMinutes} minutes.");
            await Task.Delay(skipEndCheckTime);
        }

        logger?.WriteLine("Start the video end check.");
        while (true)
        {
            var liveStreamingDetails = await youtube.GetLiveStreamingDetailsAsync(videoId);
            if (liveStreamingDetails == null)
            {
                // 配信が削除された場合
                logger?.WriteLine("The video is either private or has been removed.");
                return false;
            }

            var actualEndTime = liveStreamingDetails["actualEndTime"]?.Value<string>();
            if (!string.IsNullOrEmpty(actualEndTime))
            {
                // 配信が終了した場合
                logger?.WriteLine("The video has ended.");
                return true;
            }

            await Task.Delay(checkPollingTime);
        }
    }
}
