
namespace UnarchivedStreamRecorder.OBS;

using System.Diagnostics;
using System.Reactive.Disposables;

using UnarchivedStreamRecorder.Utilities;
using UnarchivedStreamRecorder.Utilities.Logging;

public class OBSLauncher(ILogger? logger, string filePath) : IProcessLauncher
{
    public IDisposable? Start()
    {
        if (this.ProcessExists())
        {
            logger?.WriteLine("OBS is already running.");
            return null;
        }

        logger?.WriteLine("Start OBS.");
        var process = Process.Start(
            new ProcessStartInfo
                {
                    FileName = filePath,
                    WorkingDirectory = Path.GetDirectoryName(filePath),
                });

        if (process == null)
        {
            throw new InvalidOperationException("OBS could not be started.");
        }

        process.WaitForInputIdle();
        return Disposable.Create(
            () =>
            {
                try
                {
                    if (process.HasExited)
                    {
                        return;
                    }

                    logger?.WriteLine("Close OBS.");
                    process.CloseMainWindow();
                    process.WaitForExit();
                }
                finally
                {
                    process.Dispose();
                }
            });
    }

    private bool ProcessExists()
    {
        var processName = Path.GetFileNameWithoutExtension(filePath);
        return Process.GetProcessesByName(processName).Length > 0;
    }
}
