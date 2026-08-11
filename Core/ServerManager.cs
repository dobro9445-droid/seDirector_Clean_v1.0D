using System.Diagnostics;
using System.Text.Json;
using seDirector.Models;

namespace seDirector.Core;

public sealed class ServerManager : IDisposable
{
    private readonly string _configPath;
    private readonly Logger _logger;
    private readonly object _sync = new();
    private readonly Dictionary<int, Process> _processes = new();

    public List<Server> Servers { get; private set; } = new();

    public ServerManager(string configPath, Logger logger)
    {
        _configPath = configPath;
        _logger = logger;
    }

    public void LoadServers()
    {
        try
        {
            if (!File.Exists(_configPath))
                CreateDefaultConfig();

            var json = File.ReadAllText(_configPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            Servers = JsonSerializer.Deserialize<List<Server>>(json, options) ?? new List<Server>();

            _logger.Info($"Конфигурация загружена: {Servers.Count} сервер(ов).");
        }
        catch (Exception ex)
        {
            Servers = new List<Server>();
            _logger.Error($"Не удалось загрузить конфигурацию: {ex.Message}");
        }
    }

    private void CreateDefaultConfig()
    {
        var directory = Path.GetDirectoryName(_configPath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var sample = new List<Server>
        {
            new Server
            {
                Name = "Team Fortress 2",
                Path = @"C:\Servers\TF2\srcds.exe",
                Arguments = "-console -game tf",
                AutoStart = false,
                RestartOnExit = false
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(_configPath, JsonSerializer.Serialize(sample, options));

        _logger.Info($"Создан файл конфигурации по умолчанию: {_configPath}");
    }

    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < Servers.Count;
    }

    public bool IsRunning(int index)
    {
        lock (_sync)
        {
            if (!IsValidIndex(index))
                return false;

            if (!_processes.TryGetValue(index, out var process) || process is null)
                return false;

            try
            {
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool TryStart(int index)
    {
        if (!IsValidIndex(index))
            return false;

        var server = Servers[index];

        lock (_sync)
        {
            if (_processes.TryGetValue(index, out var existing))
            {
                if (existing is not null && !HasExitedSafe(existing))
                {
                    _logger.Warning($"Сервер '{server.Name}' уже запущен (PID {GetProcessIdSafe(existing)}).");
                    return false;
                }

                DisposeProcess(existing);
                _processes.Remove(index);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(server.Path))
                {
                    _logger.Error($"Сервер '{server.Name}': не указан путь к исполняемому файлу.");
                    return false;
                }

                var fullPath = Path.GetFullPath(server.Path);

                if (!File.Exists(fullPath))
                {
                    _logger.Error($"Сервер '{server.Name}': файл не найден: {fullPath}");
                    return false;
                }

                var workingDirectory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;

                // Важно: приложение не использует скрытый запуск.
                // На Windows используется обычный видимый запуск через shell,
                // на Linux/Unix используется прямой запуск процесса.
                var useShellExecute = OperatingSystem.IsWindows();

                var startInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = server.Arguments ?? string.Empty,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = useShellExecute,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                var process = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                if (!process.Start())
                {
                    _logger.Error($"Сервер '{server.Name}': Process.Start вернул false.");
                    process.Dispose();
                    return false;
                }

                _processes[index] = process;

                _logger.Info(
                    $"Сервер '{server.Name}' запущен. " +
                    $"PID: {process.Id}. " +
                    $"Файл: {fullPath}. " +
                    $"Аргументы: {server.Arguments}"
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Сервер '{server.Name}': ошибка запуска: {ex.Message}");
                return false;
            }
        }
    }

    public bool TryStop(int index)
    {
        if (!IsValidIndex(index))
            return false;

        var server = Servers[index];

        lock (_sync)
        {
            if (!_processes.TryGetValue(index, out var process) || process is null || HasExitedSafe(process))
            {
                _logger.Warning($"Сервер '{server.Name}' не запущен.");
                return false;
            }

            try
            {
                var pid = GetProcessIdSafe(process);

                _logger.Info($"Остановка сервера '{server.Name}' (PID {pid})...");

                process.Kill(entireProcessTree: true);

                if (!process.WaitForExit(5000))
                {
                    _logger.Warning($"Сервер '{server.Name}' не завершился полностью за 5 секунд.");
                    return false;
                }

                _logger.Info($"Сервер '{server.Name}' остановлен.");

                DisposeProcess(process);
                _processes.Remove(index);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Сервер '{server.Name}': ошибка остановки: {ex.Message}");
                return false;
            }
        }
    }

    public bool TryRestart(int index)
    {
        if (!IsValidIndex(index))
            return false;

        var name = Servers[index].Name;

        _logger.Info($"Перезапуск сервера '{name}'...");

        if (IsRunning(index))
        {
            TryStop(index);
            Thread.Sleep(1000);
        }

        return TryStart(index);
    }

    public string GetStatus(int index)
    {
        if (!IsValidIndex(index))
            return "UNKNOWN";

        lock (_sync)
        {
            if (_processes.TryGetValue(index,
