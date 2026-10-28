namespace PulseMonitor.Core;

/// <summary>
/// Aynı oturumda programın tek kopya çalışmasını sağlar. <c>PulseMonitor.exe --exit</c> ile çalışan kopyaya
/// düzgün kapanma isteği gönderilir (kurulum betiği ve derleme öncesi kullanılır; veri kaybı olmaz).
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\PulseMonitor.SingleInstance";
    private const string ExitEventName = @"Local\PulseMonitor.Exit";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _exitEvent;
    private readonly RegisteredWaitHandle _exitWait;

    /// <summary>Başka bir süreç kapanma istedi (iş parçacığı havuzunda çağrılır).</summary>
    public event Action? ExitRequested;

    private SingleInstance(Mutex mutex)
    {
        _mutex = mutex;
        _exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);
        _exitWait = ThreadPool.RegisterWaitForSingleObject(
            _exitEvent, (_, _) => ExitRequested?.Invoke(), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (createdNew) return new SingleInstance(mutex);
        mutex.Dispose();
        return null;
    }

    /// <summary>Çalışan kopyaya kapanma isteği gönderir. Çalışan kopya yoksa false.</summary>
    public static bool SignalExit()
    {
        if (!EventWaitHandle.TryOpenExisting(ExitEventName, out EventWaitHandle? handle)) return false;
        using (handle) handle.Set();
        return true;
    }

    public void Dispose()
    {
        _exitWait.Unregister(null);
        _exitEvent.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
