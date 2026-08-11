namespace seDirector.Core;

public sealed class MonitorService : IDisposable
{
    private readonly ServerManager _manager;
    private readonly Logger _logger;
    private readonly object _sync = new();
    private readonly Dictionary<int, bool> _previousState = new();
    private Timer? _timer;

    public MonitorService(ServerManager manager, Logger logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public void Start(int intervalMilliseconds = 5000)
    {
        lock (_sync)
        {
            if (_timer is not null)
                return;

            RefreshState();

            _timer = new Timer(
                _ => Check(),
                null,
                intervalMilliseconds,
                intervalMilliseconds
            );

            _logger.Info($"Мониторинг состояния серверов запущен (интервал {intervalMilliseconds} мс).");
        }
    }

    private void RefreshState()
    {
        for (var i = 0; i < _manager.Servers.Count; i++)
        {
            _previousState[i] = _manager.IsRunning(i);
        }
    }

    private void Check()
    {
        if (!Monitor.TryEnter(_sync, TimeSpan.FromSeconds(2)))
            return;

        try
        {
            for (var i = 0; i < _manager.Servers.Count; i++)
            {
                var current = _manager.IsRunning(i);

                if (!_previousState.TryGetValue(i, out var previous))
                {
                    _previousState[i] = current;
                    continue;
                }

                if (previous == current)
                    continue;

                var name = _manager.Servers[i].Name;

                if (current)
                {
                    _logger.Info($"Мониторинг: сервер '{name}' запущен.");
                }
                else
                {
                    _logger.Warning($"Мониторинг: сервер '{name}' остановлен.");
                }

                _previousState[i] = current;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Мониторинг: ошибка проверки состояния: {ex.Message}");
        }
        finally
        {
            Monitor.Exit(_sync);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;

        _logger.Info("Мониторинг состояния серверов остановлен.");
    }
}
