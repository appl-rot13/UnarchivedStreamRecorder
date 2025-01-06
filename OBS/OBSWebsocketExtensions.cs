
namespace UnarchivedStreamRecorder.OBS;

using System.Reactive.Disposables;

using Newtonsoft.Json.Linq;

using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;

using UnarchivedStreamRecorder.Utilities.Extensions;

public static class OBSWebsocketExtensions
{
    public static void CreateScene(
        this IOBSWebsocket obs,
        string sceneName,
        bool setCurrent)
    {
        obs.CreateScene(sceneName);

        if (setCurrent)
        {
            obs.SetCurrentProgramScene(sceneName);
        }
    }

    public static void CreateBrowserSource(
        this IOBSWebsocket obs,
        string sceneName,
        string sourceName,
        bool? sourceEnabled,
        string? url,
        int? width,
        int? height,
        bool? reroute_audio,
        string? css)
    {
        var filterSettings = new JObject()
            .AddIfNotNull(nameof(url), url)
            .AddIfNotNull(nameof(width), width)
            .AddIfNotNull(nameof(height), height)
            .AddIfNotNull(nameof(reroute_audio), reroute_audio)
            .AddIfNotNull(nameof(css), css);

        obs.CreateInput(
            sceneName,
            sourceName,
            "browser_source",
            filterSettings,
            sourceEnabled);
    }

    public static async Task<IDisposable> CreateConnectionAsync(
        this IOBSWebsocket obs,
        string url,
        string password,
        CancellationToken cancellationToken)
    {
        await WaitForEventTrigger(
            () => obs.ConnectAsync(url, password),
            handler => obs.Connected += handler,
            handler => obs.Connected -= handler,
            cancellationToken);

        // 接続後すぐにリクエストを送信すると、NotReadyになる場合があるため少し待機
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        return Disposable.Create(obs.Disconnect);
    }

    public static Task StartRecordAsync(this IOBSWebsocket obs, CancellationToken cancellationToken) =>
        WaitForEventTrigger<RecordStateChangedEventArgs>(
            () => obs.StartRecord(),
            e => e.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STARTED,
            handler => obs.RecordStateChanged += handler,
            handler => obs.RecordStateChanged -= handler,
            cancellationToken);

    public static Task StopRecordAsync(this IOBSWebsocket obs, CancellationToken cancellationToken) =>
        WaitForEventTrigger<RecordStateChangedEventArgs>(
            () => obs.StopRecord(),
            e => e.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED,
            handler => obs.RecordStateChanged += handler,
            handler => obs.RecordStateChanged -= handler,
            cancellationToken);

    private static Task WaitForEventTrigger(
        Action eventTriggerAction,
        Action<EventHandler> addHandler,
        Action<EventHandler> removeHandler,
        CancellationToken cancellationToken) =>
        WaitForEventTrigger(eventTriggerAction, _ => true, addHandler, removeHandler, cancellationToken);

    private static async Task WaitForEventTrigger(
        Action eventTriggerAction,
        Predicate<EventArgs> predicate,
        Action<EventHandler> addHandler,
        Action<EventHandler> removeHandler,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        await using var _ = cancellationToken.Register(() => tcs.TrySetCanceled());

        EventHandler? handler = null;
        handler = (_, e) =>
        {
            if (predicate(e))
            {
                tcs.SetResult();
                removeHandler(handler!);
            }
        };

        addHandler(handler);
        eventTriggerAction();

        await tcs.Task;
    }

    private static Task WaitForEventTrigger<TEventArgs>(
        Action eventTriggerAction,
        Action<EventHandler<TEventArgs>> addHandler,
        Action<EventHandler<TEventArgs>> removeHandler,
        CancellationToken cancellationToken) =>
        WaitForEventTrigger(eventTriggerAction, _ => true, addHandler, removeHandler, cancellationToken);

    private static async Task WaitForEventTrigger<TEventArgs>(
        Action eventTriggerAction,
        Predicate<TEventArgs> predicate,
        Action<EventHandler<TEventArgs>> addHandler,
        Action<EventHandler<TEventArgs>> removeHandler,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        await using var _ = cancellationToken.Register(() => tcs.TrySetCanceled());

        EventHandler<TEventArgs>? handler = null;
        handler = (_, e) =>
        {
            if (predicate(e))
            {
                tcs.SetResult();
                removeHandler(handler!);
            }
        };

        addHandler(handler);
        eventTriggerAction();

        await tcs.Task;
    }
}
