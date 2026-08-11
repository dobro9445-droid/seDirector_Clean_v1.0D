using System.Text;

namespace seDirector.Core;

public sealed class Logger
{
    private readonly object _lock = new();
    private readonly string _logFilePath;

    public string LogFilePath => _logFilePath;

    public Logger(string baseDirectory)
    {
        var logsDirectory = Path.Combine(baseDirectory, "Logs");
        Directory.CreateDirectory(logsDirectory);

        _logFilePath = Path.Combine(logsDirectory, "app.log");
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
                File.AppendAllText(_logFilePath, line, Encoding.UTF8);
            }
            catch
            {
                // Логирование не должно приводить к аварийному завершению приложения.
            }
        }
    }

    public IReadOnlyList<string> ReadLastLines(int count)
    {
        lock (_lock)
        {
            if (!File.Exists(_logFilePath))
                return Array.Empty<string>();

            var lines = File.ReadAllLines(_logFilePath, Encoding.UTF8);

            return lines
                .Skip(Math.Max(0, lines.Length - count))
                .ToList();
        }
    }
}
