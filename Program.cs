using System.Text;
using System.Threading.Tasks;
using seDirector.Core;

namespace seDirector;

public static class Program
{
    public static int Main()
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        var basePath = ResolveBasePath();
        var logger = new Logger(basePath);
        var configPath = Path.Combine(basePath, "Config", "servers.json");

        using var manager = new ServerManager(configPath, logger);
        manager.LoadServers();
        manager.StartAutoStartServers();

        using var monitor = new MonitorService(manager, logger);
        monitor.Start();

        var backupService = new BackupService(basePath, logger);
        var rconService = new RconService(logger);
        var steamCmdService = new SteamCmdService(basePath, logger);
        var softStopService = new SoftStopService(manager, rconService, logger);

        using var scheduler = new SchedulerService(manager, backupService, logger);
        scheduler.Start();

        using var webPanel = new WebPanelService(manager, backupService, logger);
        var updateService = new UpdateService(logger);

        logger.Info("Приложение seDirector Clean v1.4 запущено.");
        var running = true;

        while (running)
        {
            PrintMenu();
            var input = Console.ReadLine();
            if (input != null) input = input.Trim();
            if (input == null) { running = false; break; }

            switch (input)
            {
                case "1": ClearScreen(); Console.WriteLine("=== Список серверов ==="); ListServers(manager); WaitKey(); break;
                case "2": PerformAction(manager, "Запустить сервер", manager.TryStart, false); WaitKey(); break;
                case "3": PerformAction(manager, "Остановить сервер", softStopService.Stop, true); WaitKey(); break;
                case "4": PerformAction(manager, "Перезапустить сервер", manager.TryRestart, true); WaitKey(); break;
                case "5": ShowServerStatus(manager); WaitKey(); break;
                case "6": ShowLogs(logger); WaitKey(); break;
                case "7": BackupSingleServer(manager, backupService); WaitKey(); break;
                case "8": BackupAllServers(manager, backupService); WaitKey(); break;
                case "9": SendRconCommand(manager, rconService); WaitKey(); break;
                case "10": UpdateServerViaSteamCmd(manager, steamCmdService); WaitKey(); break;
                case "11": ReloadConfig(manager, monitor, logger); WaitKey(); break;
                case "12": running = HandleExit(manager, logger); break;
                case "13": CheckUpdates(updateService).Wait(); WaitKey(); break;
                default: Console.WriteLine("Неверный пункт меню. Введите число от 1 до 13."); WaitKey(); break;
            }
        }

        logger.Info("Приложение seDirector Clean v1.4 завершено.");
        return 0;
    }

    private static string ResolveBasePath()
    {
        var current = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(current, "seDirector.csproj")) || Directory.Exists(Path.Combine(current, "Config")))
        {
            if (IsDirectoryWritable(current)) return current;
        }

        var baseDirectory = AppContext.BaseDirectory;
        if (IsDirectoryWritable(baseDirectory)) return baseDirectory;

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "seDirector Clean");
        Directory.CreateDirectory(appData);

        var baseConfig = Path.Combine(baseDirectory, "Config", "servers.json");
        var appDataConfig = Path.Combine(appData, "Config", "servers.json");

        if (File.Exists(baseConfig) && !File.Exists(appDataConfig))
        {
            var dir = Path.GetDirectoryName(appDataConfig);
            if (dir != null) Directory.CreateDirectory(dir);
            File.Copy(baseConfig, appDataConfig, false);
        }

        return appData;
    }

    private static bool IsDirectoryWritable(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var testFile = Path.Combine(path, Path.GetRandomFileName());
            File.WriteAllText(testFile, string.Empty);
            File.Delete(testFile);
            return true;
        }
        catch { return false; }
    }

    private static void PrintMenu()
    {
        ClearScreen();
        Console.WriteLine("================================");
        Console.WriteLine(" seDirector Clean v1.4");
        Console.WriteLine("================================");
        Console.WriteLine();
        Console.WriteLine("1. Список серверов");
        Console.WriteLine("2. Запустить сервер");
        Console.WriteLine("3. Остановить сервер");
        Console.WriteLine("4. Перезапустить сервер");
        Console.WriteLine("5. Статус сервера");
        Console.WriteLine("6. Просмотр логов");
        Console.WriteLine("7. Резервная копия сервера");
        Console.WriteLine("8. Резервное копирование всех серверов");
        Console.WriteLine("9. Отправить RCON команду");
        Console.WriteLine("10. Обновить сервер через SteamCMD");
        Console.WriteLine("11. Перечитать конфигурацию");
        Console.WriteLine("12. Выход");
        Console.WriteLine("13. Проверить обновления");
        Console.WriteLine();
        Console.Write("> ");
    }

    private static void ListServers(ServerManager manager)
    {
        if (manager.Servers.Count == 0) { Console.WriteLine("Список серверов пуст."); return; }
        Console.WriteLine();
        Console.WriteLine("N  | Статус                   | Имя                  | Путь");
        Console.WriteLine("--------------------------------------------------------------------");
        for (var i = 0; i < manager.Servers.Count; i++)
        {
            var server = manager.Servers[i];
            var status = manager.GetStatus(i);
            Console.WriteLine((i + 1).ToString().PadLeft(2) + " | " + status.PadRight(25) + " | " + Truncate(server.Name, 20).PadRight(20) + " | " + server.Path);
        }
    }

    private static void PerformAction(ServerManager manager, string title, Func<int, bool> action, bool requireConfirmation)
    {
        var index = SelectServerIndex(manager, title);
        if (index == null) return;
        if (requireConfirmation)
        {
            Console.WriteLine();
            Console.Write("Подтвердить действие? (y/N): ");
            if (!Confirm()) { Console.WriteLine("Действие отменено."); return; }
        }
        Console.WriteLine();
        var success = action(index.Value);
        Console.WriteLine(success ? "Операция выполнена успешно." : "Операция не выполнена. Подробности в логах.");
    }

    private static void ShowServerStatus(ServerManager manager)
    {
        var index = SelectServerIndex(manager, "Статус сервера");
        if (index == null) return;
        var server = manager.Servers[index.Value];
        Console.WriteLine();
        Console.WriteLine("Имя:                    " + server.Name);
        Console.WriteLine("Тип:                    " + server.Type);
        Console.WriteLine("Путь:                   " + server.Path);
        Console.WriteLine("Аргументы:              " + server.Arguments);
        Console.WriteLine("Порт:                   " + (server.Port.HasValue ? server.Port.Value.ToString() : "-"));
        if (server.Port.HasValue)
        {
            var host = "127.0.0.1";
            if (server.Rcon != null && !string.IsNullOrWhiteSpace(server.Rcon.Host)) host = server.Rcon.Host;
            var portOpen = PortChecker.IsPortOpen(host, server.Port.Value);
            Console.WriteLine("Проверка порта:         " + (portOpen ? "отвечает" : "не отвечает"));
        }
        Console.WriteLine("Приоритет:              " + server.Priority);
        Console.WriteLine("Автозапуск:             " + (server.AutoStart ? "да" : "нет"));
        Console.WriteLine("Автоперезапуск:         " + (server.RestartOnExit ? "да" : "нет"));
        Console.WriteLine("Задержка перезапуска:   " + server.RestartDelaySeconds + " сек.");
        Console.WriteLine("Лимит перезапусков:     " + server.MaxRestartAttempts);
        Console.WriteLine("Остановлен вручную:     " + (manager.IsManualStopped(index.Value) ? "да" : "нет"));
        Console.WriteLine("Статус:                 " + manager.GetStatus(index.Value));
                Console.WriteLine("Аптайм:                 " + manager.GetUptime(index.Value));
        Console.WriteLine("Память:                 " + manager.GetMemoryUsage(index.Value));
    }

    private static void BackupSingleServer(ServerManager manager, BackupService backupService)
    {
        var index = SelectServerIndex(manager, "Резервная копия сервера");
        if (index == null) return;
        Console.WriteLine();
        Console.Write("Создать резервную копию этого сервера? (y/N): ");
        if (!Confirm()) { Console.WriteLine("Отменено."); return; }
        Console.WriteLine("Создание резервной копии...");
        var success = backupService.BackupServer(manager.Servers[index.Value]);
        Console.WriteLine(success ? "Резервная копия создана." : "Не удалось создать резервную копию.");
    }

    private static void BackupAllServers(ServerManager manager, BackupService backupService)
    {
        ClearScreen();
        Console.WriteLine("=== Резервное копирование всех серверов ===");
        Console.WriteLine("Будут скопированы только серверы с Backup.Enabled = true.");
        Console.Write("Продолжить? (y/N): ");
        if (!Confirm()) { Console.WriteLine("Отменено."); return; }
        Console.WriteLine("Создание резервных копий...");
        var count = backupService.BackupAll(manager.Servers);
        Console.WriteLine("Создано резервных копий: " + count);
    }

    private static void SendRconCommand(ServerManager manager, RconService rconService)
    {
        var index = SelectServerIndex(manager, "Отправить RCON команду");
        if (index == null) return;
        var server = manager.Servers[index.Value];
        Console.WriteLine();
        Console.Write("Введите команду для сервера '" + server.Name + "': ");
        var command = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(command)) { Console.WriteLine("Команда не может быть пустой."); return; }
        Console.WriteLine("Отправка команды...");
        string response;
        var success = rconService.SendCommand(server, command, out response);
        Console.WriteLine();
        Console.WriteLine("Результат: " + (success ? "Успешно" : "Ошибка"));
        if (!string.IsNullOrWhiteSpace(response)) { Console.WriteLine(); Console.WriteLine("Ответ сервера:"); Console.WriteLine(response); }
    }

    private static void UpdateServerViaSteamCmd(ServerManager manager, SteamCmdService steamCmdService)
    {
        var index = SelectServerIndex(manager, "Обновить сервер через SteamCMD");
        if (index == null) return;
        var server = manager.Servers[index.Value];
        Console.WriteLine();
        if (!steamCmdService.IsAvailable()) { Console.WriteLine("SteamCMD не найден."); return; }
        if (manager.IsRunning(index.Value))
        {
            Console.WriteLine("Сервер запущен. Остановить и продолжить? (y/N): ");
            if (!Confirm()) { Console.WriteLine("Отменено."); return; }
            manager.TryStop(index.Value);
        }
        Console.Write("Обновить сервер '" + server.Name + "' через SteamCMD? (y/N): ");
        if (!Confirm()) { Console.WriteLine("Отменено."); return; }
        Console.WriteLine("Запуск SteamCMD...");
        var success = steamCmdService.UpdateServer(server);
        Console.WriteLine(success ? "Обновление завершено." : "Ошибка обновления.");
    }

    private static int? SelectServerIndex(ServerManager manager, string title)
    {
        ClearScreen();
        Console.WriteLine("=== " + title + " ===");
        ListServers(manager);
        if (manager.Servers.Count == 0) return null;
        Console.WriteLine();
        Console.Write("Введите номер сервера (0 - отмена): ");
        var input = Console.ReadLine();
        if (input != null) input = input.Trim();
        int number;
        if (!int.TryParse(input, out number)) { Console.WriteLine("Некорректный номер сервера."); return null; }
        if (number == 0) return null;
        var index = number - 1;
        if (!manager.IsValidIndex(index)) { Console.WriteLine("Сервер с таким номером не найден."); return null; }
        return index;
    }

    private static void ShowLogs(Logger logger)
    {
        ClearScreen();
        Console.WriteLine("=== Последние 100 записей лога ===");
        Console.WriteLine();
        var lines = logger.ReadLastLines(100);
        if (lines.Count == 0) { Console.WriteLine("Логи пусты."); return; }
        foreach (var line in lines) Console.WriteLine(line);
    }

    private static void ReloadConfig(ServerManager manager, MonitorService monitor, Logger logger)
    {
        ClearScreen();
        Console.WriteLine("Перечитать конфигурацию servers.json?");
        Console.WriteLine("Все запущенные серверы будут остановлены.");
        Console.Write("Продолжить? (y/N): ");
        if (!Confirm()) { Console.WriteLine("Отменено."); return; }
        manager.StopAll();
        manager.LoadServers();
        monitor.Refresh();
        logger.Info("Конфигурация перечитана пользователем.");
        Console.WriteLine();
        Console.WriteLine("Конфигурация перечитана.");
    }

    private static bool HandleExit(ServerManager manager, Logger logger)
    {
        ClearScreen();
        Console.WriteLine("Вы действительно хотите выйти? (y/N)");
        if (!Confirm()) return false;
        Console.WriteLine("Остановить все запущенные серверы перед выходом? (y/N)");
        if (Confirm()) { manager.StopAll(); Console.WriteLine("Все серверы остановлены."); }
        return true;
    }

    private static bool Confirm()
    {
        var answer = Console.ReadLine();
        if (answer != null) answer = answer.Trim().ToLowerInvariant();
        return answer == "y" || answer == "yes" || answer == "д" || answer == "да";
    }

    private static void WaitKey()
    {
        Console.WriteLine();
        Console.Write("Нажмите Enter для продолжения...");
        Console.ReadLine();
    }

    private static void ClearScreen()
    {
        try { Console.Clear(); } catch { }
    }

    private static string Truncate(string value, int length)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (value.Length <= length) return value;
        return value.Substring(0, length);
    }

    private static async Task CheckUpdates(UpdateService updateService)
    {
        ClearScreen();
        Console.WriteLine("Проверка обновлений на GitHub...");
        var result = await updateService.CheckForUpdatesAsync();
        Console.WriteLine();
        Console.WriteLine(result);
    }
}
