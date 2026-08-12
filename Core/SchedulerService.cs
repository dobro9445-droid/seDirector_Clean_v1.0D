using seDirector.Models;

namespace seDirector.Core;

public sealed class SchedulerService : IDisposable
{
    private readonly ServerManager _manager;
    private readonly BackupService _backupService;
    private readonly Logger _logger;

    private readonly object _sync = new object();
    private readonly Dictionary<string, DateTime> _lastExecuted = new Dictionary<string, DateTime>();

    private System.Threading.Timer? _timer;

    public SchedulerService(ServerManager manager, BackupService backupService, Logger logger)
    {
        _manager = manager;
        _backupService = backupService;
        _logger = logger;
    }

    public void Start(int intervalMilliseconds = 30000)
    {
        lock (_sync)
        {
            if (_timer != null)
                return;

            _timer = new System.Threading.Timer(
                Check,
                null,
                intervalMilliseconds,
                intervalMilliseconds
            );

            _logger.Info("Планировщик задач запущен (интервал " + intervalMilliseconds + " мс).");
        }
    }

    private void Check(object? state)
    {
        if (!Monitor.TryEnter(_sync, TimeSpan.FromSeconds(2)))
            return;

        try
        {
            var now = DateTime.Now;

            for (var i = 0; i < _manager.Servers.Count; i++)
            {
                var server = _manager.Servers[i];
                var schedule = server.Schedule;

                if (schedule == null || !schedule.Enabled)
                    continue;

                TimeSpan time;

                if (!string.IsNullOrWhiteSpace(schedule.Start) &&
                    TimeSpan.TryParse(schedule.Start, out time) &&
                    Matches(now, time))
                {
                    Execute(i, "start", now, () =>
                    {
                        _logger.Info("Планировщик: запуск сервера '" + server.Name + "'.");
                        _manager.TryStart(i);
                    });
                }

                if (!string.IsNullOrWhiteSpace(schedule.Stop) &&
                    TimeSpan.TryParse(schedule.Stop, out time) &&
                    Matches(now, time))
                {
                    Execute(i, "stop", now, () =>
                    {
                        _logger.Info("Планировщик: остановка сервера '" + server.Name + "'.");
                        _manager.TryStop(i);
                    });
                }

                if (!string.IsNullOrWhiteSpace(schedule.Restart) &&
                    TimeSpan.TryParse(schedule.Restart, out time) &&
                    Matches(now, time))
                {
                    Execute(i, "restart", now, () =>
                    {
                        _logger.Info("Планировщик: перезапуск сервера '" + server.Name + "'.");
                        _manager.TryRestart(i);
                    });
                }

                if (!string.IsNullOrWhiteSpace(schedule.Backup) &&
                    TimeSpan.TryParse(schedule.Backup, out time) &&
                    Matches(now, time))
                {
                    Execute(i, "backup", now, () =>
                    {
                        _logger.Info("Планировщик: резервное копирование сервера '" + server.Name + "'.");
                        _backupService.BackupServer(server);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Планировщик: ошибка проверки задач: " + ex.Message);
        }
        finally
        {
            Monitor.Exit(_sync);
        }
    }

    private void Execute(int index, string task, DateTime now, Action action)
    {
        var slot = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
        var key = index + "_" + task;

        lock (_sync)
        {
            DateTime last;

            if (_lastExecuted.TryGetValue(key, out last) && last == slot)
                return;

            _lastExecuted[key] = slot;
        }

        try
        {
            action();
        }
        catch (Exception ex)
        {
            _logger.Error("Планировщик: ошибка задачи '" + task + "': " + ex.Message);
        }
    }

    private static bool Matches(DateTime now, TimeSpan time)
    {
        return now.Hour == time.Hours && now.Minute == time.Minutes;
    }

    public void Dispose()
    {
        if (_timer != null)
        {
            _timer.Dispose();
            _timer = null;
        }

        _logger.Info("Планировщик задач остановлен.");
    }
}
