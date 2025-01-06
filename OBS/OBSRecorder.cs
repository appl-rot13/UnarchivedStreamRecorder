
namespace UnarchivedStreamRecorder.OBS;

using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;

using UnarchivedStreamRecorder.Utilities;
using UnarchivedStreamRecorder.Utilities.Logging;

public class OBSRecorder(
    ILogger? logger,
    IProcessLauncher launcher,
    IOBSWebsocket websocket,
    string url,
    string password,
    string sceneName,
    string sourceName)
{
    public async Task RecordAsync(ILiveStream liveStream)
    {
        if (!await liveStream.WaitForStart())
        {
            // 録画開始前に配信が削除された場合
            return;
        }

        using var obs = launcher.Start();
        using var cts = this.CreateCancellationTokenSource();

        try
        {
            using var connection = await websocket.CreateConnectionAsync(url, password, cts.Token);

            websocket.CreateScene(sceneName, true);
            websocket.CreateBrowserSource(sceneName, sourceName, true, liveStream.Url, 1920, 1080, true, null);

            logger?.WriteLine("Start recording.");
            await websocket.StartRecordAsync(cts.Token);

            try
            {
                await liveStream.WaitForEnd();
            }
            finally
            {
                logger?.WriteLine("Stop recording.");
                await websocket.StopRecordAsync(cts.Token);

                websocket.RemoveInput(sourceName);
                websocket.RemoveScene(sceneName);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private CancellationTokenSource CreateCancellationTokenSource()
    {
        var cts = new CancellationTokenSource();

        EventHandler<ObsDisconnectionInfo>? handler = null;
        handler = (_, e) =>
        {
            var closeCode = (int)e.ObsCloseCode;
            if (closeCode != 1000)
            {
                var details = $"Code: {closeCode}";
                if (!string.IsNullOrWhiteSpace(e.DisconnectReason))
                {
                    details += $", Reason: {e.DisconnectReason}";
                }

                logger?.WriteLine($"Disconnected from OBS. {details}");
                cts.Cancel();
            }

            websocket.Disconnected -= handler;
        };

        websocket.Disconnected += handler;
        return cts;
    }
}
