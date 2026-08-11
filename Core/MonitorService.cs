using System;
using System.Collections.Generic;
using System.Threading;

namespace seDirector.Core;

public sealed class MonitorService : IDisposable
{
    private readonly ServerManager _manager;
    private readonly Logger _logger;
    private readonly object _sync = new object();

    private readonly Dictionary<int, bool> _previousState = new Dictionary<int, bool>();
    private readonly HashSet<int> _autoRestartPending = new HashSet<int>();

    private Timer _timer;

    public MonitorService(ServerManager manager, Logger logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public void Start(int intervalMilliseconds = 5000)
    {
        lock (_sync)
        {
            if (_timer != null)
                return;

            RefreshState();

            _timer = new Timer(
                Check,
                null,
                intervalMilliseconds,
                intervalMilliseconds
            );

            _logger.Info("Мониторинг состояния серверов запущен (интервал " + intervalMilliseconds + " мс).");
        }
    }

    public void Refresh()
    {
        lock (_sync)
        {
            _previousState.Clear();
            _autoRestartPending.Clear();
            RefreshState();
            _logger.Info("Состояние мониторинга обновлено.");
        }
    }

    private void RefreshState()
    {
        for (var i = 0; i < _manager.Servers.Count; i++)
        {
            _previousState[i] = _manager.IsRunning(i);
        }
    }

    private void Check(object state)
    {
        if (!Monitor.TryEnter(_sync, TimeSpan.FromSeconds(2)))
            return;

        try
        {
            for (var i = 0; i < _manager.Servers.Count; i++)
            {
                var current = _manager.IsRunning(i);

                bool previous;

                if (!_previousState.TryGetValue(i, out previous))
                {
                    _previousState[i] = current;
                    continue;
                }

                if (previous != current)
                {
                    var name = _manager.Servers[i].Name;

                    if (current)
                    {
                        _logger.Info("Мониторинг: сервер '" + name + "' запущен.");
                    }
                    else
                    {
                        _logger.Warning("Мониторинг: сервер '" + name + "' остановлен.");

                        var server = _manager.Servers[i];

                        if (server.RestartOnExit && !_manager.IsManualStopped(i))
                        {
                            _autoRestartPending.Add(i);
                        }
                    }

                    _previousState[i] = current;
                }

                if (_autoRestartPending.Contains(i))
                {
                    if (current ||
                        _manager.IsManualStopped(i) ||
                        !_manager.Servers[i].RestartOnExit ||
                        _manager.HasReachedAutoRestartLimit(i))
                    {
                        _autoRestartPending.Remove(i);
                    }
                    else if (_manager.CanAutoRestart(i))
                    {
                        _manager.TryAutoRestart(i);

                        if (_manager.IsRunning(i))
                        {
                            _autoRestartPending.Remove(i);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Мониторинг: ошибка проверки состояния: " + ex.Message);
        }
        finally
        {
            Monitor.Exit(_sync);
        }
    }

    public void Dispose()
    {
        if (_timer != null)
        {
            _timer.Dispose();
            _timer = null;
        }

        _logger.Info("Мониторинг состояния серверов остановлен.");
    }
}
