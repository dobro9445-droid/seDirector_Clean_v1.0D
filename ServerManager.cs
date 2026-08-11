using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using seDirector.Models;

namespace seDirector.Core
{
    public sealed class ServerManager : IDisposable
    {
        private readonly string _configPath;
        private readonly Logger _logger;
        private readonly object _sync = new object();
        private readonly Dictionary<int, Process> _processes = new Dictionary<int, Process>();

        public List<Server> Servers { get; private set; } = new List<Server>();

        public ServerManager(string configPath, Logger logger)
        {
            _configPath = configPath;
            _logger = logger;
        }

        public void LoadServers()
        {
            try
            {
                if (!File.Exists(_configPath)) CreateDefaultConfig();
                var json = File.ReadAllText(_configPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
                var deserialized = JsonSerializer.Deserialize<List<Server>>(json, options);
                Servers = deserialized ?? new List<Server>();
                _logger.Info("Конфигурация загружена: " + Servers.Count + " сервер(ов).");
            }
            catch (Exception ex)
            {
                Servers = new List<Server>();
                _logger.Error("Не удалось загрузить конфигурацию: " + ex.Message);
            }
        }

        private void CreateDefaultConfig()
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

            var sample = new List<Server>
            {
                new Server { Name = "Team Fortress 2", Path = @"C:\Servers\TF2\srcds.exe", Arguments = "-console -game tf", AutoStart = false, RestartOnExit = false }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_configPath, JsonSerializer.Serialize(sample, options));
            _logger.Info("Создан файл конфигурации по умолчанию: " + _configPath);
        }

        public bool IsValidIndex(int index) { return index >= 0 && index < Servers.Count; }

        public bool IsRunning(int index)
        {
            lock (_sync)
            {
                if (!IsValidIndex(index)) return false;
                Process process;
                if (!_processes.TryGetValue(index, out process) || process == null) return false;
                try { return !process.HasExited; } catch { return false; }
            }
        }

        public bool TryStart(int index)
        {
            if (!IsValidIndex(index)) return false;
            var server = Servers[index];

            lock (_sync)
            {
                Process existing;
                if (_processes.TryGetValue(index, out existing))
                {
                    if (existing != null && !HasExitedSafe(existing))
                    {
                        _logger.Warning("Сервер '" + server.Name + "' уже запущен.");
                        return false;
                    }
                    DisposeProcess(existing);
                    _processes.Remove(index);
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(server.Path)) { _logger.Error("Не указан путь к файлу."); return false; }
                    var fullPath = Path.GetFullPath(server.Path);
                    if (!File.Exists(fullPath)) { _logger.Error("Файл не найден: " + fullPath); return false; }

                    var workingDirectory = Path.GetDirectoryName(fullPath);
                    if (string.IsNullOrWhiteSpace(workingDirectory)) workingDirectory = Environment.CurrentDirectory;

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = fullPath,
                        Arguments = server.Arguments ?? string.Empty,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Normal
                    };

                    var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                    if (!process.Start()) { process.Dispose(); return false; }

                    _processes[index] = process;
                    _logger.Info("Сервер '" + server.Name + "' запущен. PID: " + process.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка запуска: " + ex.Message);
                    return false;
                }
            }
        }

        public bool TryStop(int index)
        {
            if (!IsValidIndex(index)) return false;
            var server = Servers[index];

            lock (_sync)
            {
                Process process;
                if (!_processes.TryGetValue(index, out process) || process == null || HasExitedSafe(process))
                {
                    _logger.Warning("Сервер '" + server.Name + "' не запущен.");
                    return false;
                }

                try
                {
                    _logger.Info("Остановка сервера '" + server.Name + "'...");
                    try { process.Kill(true); } catch { process.Kill(); }

                    if (!process.WaitForExit(5000)) { _logger.Warning("Сервер не завершился за 5 секунд."); return false; }

                    _logger.Info("Сервер '" + server.Name + "' остановлен.");
                    DisposeProcess(process);
                    _processes.Remove(index);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка остановки: " + ex.Message);
                    return false;
                }
            }
        }

        public bool TryRestart(int index)
        {
            if (!IsValidIndex(index)) return false;
            if (IsRunning(index)) { TryStop(index); Thread.Sleep(1000); }
            return TryStart(index);
        }

        public string GetStatus(int index)
        {
            if (!IsValidIndex(index)) return "UNKNOWN";
            lock (_sync)
            {
                Process process;
                if (_processes.TryGetValue(index, out process) && process != null)
                {
                    try
                    {
                        if (!process.HasExited) return "RUNNING (PID " + process.Id + ")";
                        return "STOPPED (Exit code: " + process.ExitCode + ")";
                    }
                    catch { return "UNKNOWN"; }
                }
            }
            return "STOPPED";
        }

        public void StartAutoStartServers()
        {
            for (var i = 0; i < Servers.Count; i++) { if (Servers[i].AutoStart) TryStart(i); }
        }

        public void StopAll()
        {
            for (var i = 0; i < Servers.Count; i++) { if (IsRunning(i)) TryStop(i); }
        }

        private static bool HasExitedSafe(Process process) { try { return process.HasExited; } catch { return true; } }
        private static void DisposeProcess(Process process) { try { if (process != null) process.Dispose(); } catch { } }

        public void Dispose()
        {
            lock (_sync)
            {
                foreach (var process in _processes.Values) DisposeProcess(process);
                _processes.Clear();
            }
        }
    }
}
