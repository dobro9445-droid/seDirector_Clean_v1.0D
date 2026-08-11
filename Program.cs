using System.Text;
using System.Threading.Tasks;
using seDirector.Core;

namespace seDirector;

public static class Program
{
    public static int Main()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch
        {
            // Если консоль не поддерживает UTF-8, продолжаем без изменения кодировки.
        }

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

            if (input != null)
                input = input.Trim();

            if (input == null)
            {
                running = false;
                break;
            }

            switch (input)
            {
                case "1":
                    ClearScreen();
                    Console.WriteLine("=== Список серверов ===");
                    ListServers(manager);
                    WaitKey();
                    break;

                case "2":
                    PerformAction(manager, "Запустить сервер", manager.TryStart, false);
                    WaitKey();
                    break;

                case "3":
                    PerformAction(manager, "Остановить сервер", softStopService.Stop, true);
                    WaitKey();
                    break;

                case "4":
                    PerformAction(manager, "Перезапустить сервер", manager.TryRestart, true);
                    WaitKey();
                    break;

                case "5":
                    ShowServerStatus(manager);
                    WaitKey();
                    break;

                case "6":
                    ShowLogs(logger);
                    WaitKey();
                    break;

                case "7":
                    BackupSingleServer(manager, backupService);
                    WaitKey();
                    break;

                case "8":
                    BackupAllServers(manager, backupService);
                    WaitKey();
                    break;

                case "9":
                    SendRconCommand(manager, rconService);
                    WaitKey();
                    break;

                case "10":
                    UpdateServerViaSteamCmd(manager, steamCmdService);
                    WaitKey();
                    break;

                case "11":
                    ReloadConfig(manager, monitor, logger);
                    WaitKey();
                    break;

                case "12":
                    running = HandleExit(manager, logger);
                    break;

                case "13":
                    CheckUpdates(updateService).Wait();
                    WaitKey();
                    break;

                default:
                    Console.WriteLine("Неверный пункт меню. Введите число от 1 до 13.");
                    WaitKey();
                    break;
            }
        }

        logger.Info("Приложение seDirector Clean v1.4 завершено.");
        return 0;
    }

    private static string ResolveBasePath()
    {
        var current = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(current, "seDirector.csproj")) ||
            Directory.Exists(Path.Combine(current, "Config")))
        {
            if (IsDirectoryWritable(current))
                return current;
        }

        var baseDirectory = AppContext.BaseDirectory;

        if (IsDirectoryWritable(baseDirectory))
            return baseDirectory;

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "seDirector Clean"
        );

        Directory.CreateDirectory(appData);

        var baseConfig = Path.Combine(baseDirectory, "Config", "servers.json");
        var appDataConfig = Path.Combine(appData, "Config", "servers.json");

        if (File.Exists(baseConfig) && !File.Exists(appDataConfig))
        {
            var dir = Path.GetDirectoryName(appDataConfig);

            if (dir != null)
                Directory.CreateDirectory(dir);

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
        catch
        {
            return false;
        }
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
        Console
