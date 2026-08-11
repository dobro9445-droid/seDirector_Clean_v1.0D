using System.Text;

namespace seDirector.Core;

public sealed class Logger
{
    private readonly object _lock = new();
    private readonly string _logsDirectory;
    private readonly string _logFilePath;

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxArchiveCount = 3;

    public string LogFilePath => _logFilePath;

    public Logger(string baseDirectory)
    {
        _logsDirectory = Path.Combine(baseDirectory, "Logs");
        Directory.CreateDirectory(_logsDirectory);
        _logFilePath = Path.Combine(_logsDirectory, "app.log");
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";

        lock (_lock)
        {
            try
            {
                RotateIfNeeded();
                File.AppendAllText(_logFilePath, line, Encoding.UTF8);
            }
            catch
            {
                // Логирование не должно приводить к аварийному завершению приложения.
            }
        }
    }

    private void RotateIfNeeded()
    {
        try
        {
            var fileInfo = new FileInfo(_logFilePath);

            if (!fileInfo.Exists || fileInfo.Length < MaxFileSizeBytes)
                return;

            var oldest = Path.Combine(_logsDirectory, $"app.{MaxArchiveCount}.log");

            if (File.Exists(oldest))
                File.Delete(oldest);

            for (var i = MaxArchiveCount - 1; i >= 1; i--)
            {
                var source = Path.Combine(_logsDirectory, $"app.{i}.log");
                var destination = Path.Combine(_logsDirectory, $"app.{i + 1}.log");

                if (File.Exists(source))
                    File.Move(source, destination);
            }

            File.Move(_logFilePath, Path.Combine(_logsDirectory, "app.1.log"));
        }
        catch
        {
            // Если ротация не удалась, продолжаем писать в текущий лог.
        }
    }

    public IReadOnlyList<string> ReadLastLines(int count)
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_logFilePath))
                    return Array.Empty<string>();

                var lines = File.ReadAllLines(_logFilePath, Encoding.UTF8);

                return lines
                    .Skip(Math.Max(0, lines.Length - count))
                    .ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
