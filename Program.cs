using System.Text;
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

        logger.Info("Приложение seDirector Clean v1.0 запущено.");

        var running = true;

        while (running)
        {
            PrintMenu();

            var input = Console.ReadLine()?.Trim();

            if (input is null)
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
                    PerformAction(manager, "Запустить сервер", manager.TryStart);
                    WaitKey();
                    break;

                case "3":
                    PerformAction(manager, "Остановить сервер", manager.TryStop);
                    WaitKey();
                    break;

                case "4":
                    PerformAction(manager, "Перезапустить сервер", manager.TryRestart);
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
                    running = HandleExit(manager, logger);
                    break;

                default:
                    Console.WriteLine("Неверный пункт меню. Введите число от 1 до 7.");
                    WaitKey();
                    break;
            }
        }

        logger.Info("Приложение seDirector Clean v1.0 завершено.");
        return 0;
    }

    private static string ResolveBasePath()
    {
        var current = Directory.GetCurrentDirectory();

        // Режим разработки: запуск через dotnet run из папки проекта.
        if (File.Exists(Path.Combine(current, "seDirector.csproj")) ||
            Directory.Exists(Path.Combine(current, "Config")))
        {
            if (IsDirectoryWritable(current))
                return current;
        }

        // Папка приложения.
        var baseDirectory = AppContext.BaseDirectory;

        if (IsDirectoryWritable(baseDirectory))
            return baseDirectory;

        // Если папка программы защищена, используем пользовательские данные.
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "seDirector Clean"
        );

        Directory.CreateDirectory(appData);

        var baseConfig = Path.Combine(baseDirectory, "Config", "servers.json");
        var appDataConfig = Path.Combine(appData, "Config", "servers.json");

        if (File.Exists(baseConfig) && !File.Exists(appDataConfig))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(appDataConfig)!);
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
        Console.WriteLine(" seDirector Clean v1.0");
        Console.WriteLine("================================");
        Console.WriteLine();
        Console.WriteLine("1. Список серверов");
        Console.WriteLine("2. Запустить сервер");
        Console.WriteLine("3. Остановить сервер");
        Console.WriteLine("4. Перезапустить сервер");
        Console.WriteLine("5. Статус сервера");
        Console.WriteLine("6. Просмотр логов");
        Console.WriteLine("7. Выход");
        Console.WriteLine();
        Console.Write("> ");
    }

    private static void ListServers(ServerManager manager)
    {
        if (manager.Servers.Count == 0)
        {
            Console.WriteLine("Список серверов пуст. Проверьте Config/servers.json.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("N  | Статус                   | Имя                  | Путь");
        Console.WriteLine("--------------------------------------------------------------------");

        for (var i = 0; i < manager.Servers.Count; i++)
        {
            var server = manager.Servers[i];
            var status = manager.GetStatus(i);

            Console.WriteLine(
                $"{i + 1,2} | {status,-25} | {Truncate(server.Name, 20),-20} | {server.Path}"
            );
        }
    }

    private static void PerformAction(ServerManager manager, string title, Func<int, bool> action)
    {
        var index = SelectServerIndex(manager, title);

        if (index is null)
            return;

        Console.WriteLine();

        var success = action(index.Value);

        Console.WriteLine(success
            ? "Операция выполнена успешно."
            : "Операция не выполнена или не требуется. Подробности смотрите в логах.");
    }

    private static void ShowServerStatus(ServerManager manager)
    {
        var index = SelectServerIndex(manager, "Статус сервера");

        if (index is null)
            return;

        var server = manager.Servers[index.Value];

        Console.WriteLine();
        Console.WriteLine($"Имя:        {server.Name}");
        Console.WriteLine($"Путь:       {server.Path}");
        Console.WriteLine($"Аргументы:  {server.Arguments}");
        Console.WriteLine($"Автозапуск: {(server.AutoStart ? "да" : "нет")}");
        Console.WriteLine($"Статус:     {manager.GetStatus(index.Value)}");
    }

    private static int? SelectServerIndex(ServerManager manager, string title)
    {
        ClearScreen();

        Console.WriteLine($"=== {title} ===");
        ListServers(manager);

        if (manager.Servers.Count == 0)
            return null;

        Console.WriteLine();
        Console.Write("Введите номер сервера (0 - отмена): ");

        var input = Console.ReadLine()?.Trim();

        if (!int.TryParse(input, out var number))
        {
            Console.WriteLine("Некорректный номер сервера.");
            return null;
        }

        if (number == 0)
            return null;

        var index = number - 1;

        if (!manager.IsValidIndex(index))
        {
            Console.WriteLine("Сервер с таким номером не найден.");
            return null;
        }

        return index;
    }

    private static void ShowLogs(Logger logger)
    {
        ClearScreen();

        Console.WriteLine("=== Последние 100 записей лога ===");
        Console.WriteLine();

        var lines = logger.ReadLastLines(100);

        if (lines.Count == 0)
        {
            Console.WriteLine("Логи пусты или файл ещё не создан.");
            return;
        }

        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
    }

    private static bool HandleExit(ServerManager manager, Logger logger)
    {
        ClearScreen();

        Console.WriteLine("Вы действительно хотите выйти? (y/N)");

        var answer = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (answer != "y" && answer != "yes" && answer != "д" && answer != "да")
            return false;

        Console.WriteLine("Остановить все запущенные серверы перед выходом? (y/N)");

        var stopAnswer = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (stopAnswer == "y" || stopAnswer == "yes" || stop
