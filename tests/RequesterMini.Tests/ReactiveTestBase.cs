using System.Reactive.Concurrency;
using ReactiveUI.Reactive;
using ReactiveUI.Reactive.Builder;

namespace RequesterMini.Tests;

/// <summary>
/// Shared setup for ViewModel tests. ReactiveUI 24 refuses to run until it has been built, which
/// the app does at startup and a test host does not. The "main thread" scheduler is then pointed at
/// the current one, since outside the app there is no UI thread to pump the work every
/// <c>ObserveOn</c> in a ViewModel constructor would otherwise queue.
/// </summary>
public abstract class ReactiveTestBase
{
    private static readonly object Gate = new();
    private static bool _initialized;

    protected ReactiveTestBase()
    {
        lock (Gate)
        {
            if (!_initialized)
            {
                RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();
                _initialized = true;
            }
        }

        RxSchedulers.MainThreadScheduler = CurrentThreadScheduler.Instance;
    }

    /// <summary>
    /// A temp directory of its own, so a ViewModel that persists to disk never touches the files
    /// the real app uses.
    /// </summary>
    protected static string NewDataDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "RequesterMini.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    protected static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
