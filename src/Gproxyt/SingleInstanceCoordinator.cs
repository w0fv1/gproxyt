namespace Gproxyt;

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly EventWaitHandle activationEvent;
    private RegisteredWaitHandle? listener;

    public SingleInstanceCoordinator(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        activationEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            $"Local\\{applicationId}-activate",
            out var createdNew);
        IsPrimary = createdNew;
    }

    public bool IsPrimary { get; }

    public void StartListening(Action activationRequested)
    {
        ArgumentNullException.ThrowIfNull(activationRequested);
        if (!IsPrimary)
        {
            throw new InvalidOperationException("只有主实例可以监听窗口激活请求。");
        }
        if (listener is not null)
        {
            throw new InvalidOperationException("窗口激活监听已经启动。");
        }

        listener = ThreadPool.RegisterWaitForSingleObject(
            activationEvent,
            (_, timedOut) =>
            {
                if (!timedOut)
                {
                    activationRequested();
                }
            },
            null,
            Timeout.Infinite,
            false);
    }

    public void NotifyPrimary()
    {
        if (IsPrimary)
        {
            throw new InvalidOperationException("主实例不能通知自身。");
        }
        activationEvent.Set();
    }

    public void Dispose()
    {
        listener?.Unregister(null);
        activationEvent.Dispose();
    }
}
