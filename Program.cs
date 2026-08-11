using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using seDirector.Core;
using seDirector.Models;

namespace seDirector
{
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

            logger.Info("Приложение seDirector Clean v1.0 запущено.");
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
                    case "2": PerformAction(manager, "Запустить сервер", manager.TryStart); WaitKey(); break;
                    case "3": PerformAction(manager, "Остановить сервер", manager.TryStop); WaitKey(); break;
                    case "4": PerformAction(manager, "Перезапустить сервер", manager.TryRestart); WaitKey(); break;
                    case "5": ShowServerStatus(manager); WaitKey(); break;
                    case "6": ShowLogs(logger); WaitKey(); break;
                    case "7": running = HandleExit(manager, logger); break;
                    default: Console.WriteLine("Неверный пункт меню. Введите число от 1 до 7."); WaitKey(); break;
                }
            }

            logger.Info("Приложение seDirector Clean v1.0 завершено.");
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
            Console.WriteLine(" seDirector Clean v1.0");
            Console.WriteLine("================================");
            Console.WriteLine("\n1. Список серверов\n2. Запустить сервер\n3. Остановить сервер");
            Console.WriteLine("4. Перезапустить сервер\n5. Статус сервера\n6. Просмотр логов\n7. Выход\n");
            Console.Write("> ");
        }

        private static void ListServers(ServerManager manager)
        {
            if (manager.Servers.Count == 0) { Console.WriteLine("Список серверов пуст."); return; }
            Console.WriteLine("\nN  | Статус                   | Имя                  | Путь");
            Console.WriteLine("--------------------------------------------------------------------");
            for (var i = 0; i < manager.Servers.Count; i++)
            {
                var server = manager.Servers[i];
                var status = manager.GetStatus(i);
                Console.WriteLine(string.Format("{0,2} | {1,-25} | {2,-20} | {3}", i + 1, status, Truncate(server.Name, 20), server.Path));
            }
        }

        private static void PerformAction(ServerManager manager, string title, Func<int, bool> action)
        {
            int? index = SelectServerIndex(manager, title);
            if (index == null) return;
            Console.WriteLine();
            var success = action(index.Value);
            Console.WriteLine(success ? "Операция выполнена успешно." : "Ошибка. Смотрите логи.");
        }

        private static void ShowServerStatus(ServerManager manager)
        {
            int? index = SelectServerIndex(manager, "Статус сервера");
            if (index == null) return;
            var server = manager.Servers[index.Value];
            Console.WriteLine("\nИмя:        " + server.Name);
            Console.WriteLine("Путь:       " + server.Path);
            Console.WriteLine("Аргументы:  " + server.Arguments);
            Console.WriteLine("Автозапуск: " + (server.AutoStart ? "да" : "нет"));
            Console.WriteLine("Статус:     " + manager.GetStatus(index.Value));
        }

        private static int? SelectServerIndex(ServerManager manager, string title)
        {
            ClearScreen();
            Console.WriteLine("=== " + title + " ===");
            ListServers(manager);
            if (manager.Servers.Count == 0) return null;
            Console.Write("\nВведите номер сервера (0 - отмена): ");
            var input = Console.ReadLine();
            if (input != null) input = input.Trim();
            int number;
            if (!int.TryParse(input, out number)) { Console.WriteLine("Некорректный номер."); return null; }
            if (number == 0) return null;
            var index = number - 1;
            if (!manager.IsValidIndex(index)) { Console.WriteLine("Сервер не найден."); return null; }
            return index;
        }

        private static void ShowLogs(Logger logger)
        {
            ClearScreen();
            Console.WriteLine("=== Последние 100 записей лога ===\n");
            var lines = logger.ReadLastLines(100);
            if (lines.Count == 0) { Console.WriteLine("Логи пусты."); return; }
            foreach (var line in lines) Console.WriteLine(line);
        }

        private static bool HandleExit(ServerManager manager, Logger logger)
        {
            ClearScreen();
            Console.WriteLine("Выйти? (y/N)");
            var answer = Console.ReadLine();
            if (answer != null) answer = answer.Trim().ToLowerInvariant();
            if (answer != "y" && answer != "yes" && answer != "д" && answer != "да") return false;

            Console.WriteLine("Остановить серверы перед выходом? (y/N)");
            var stopAnswer = Console.ReadLine();
            if (stopAnswer != null) stopAnswer = stopAnswer.Trim().ToLowerInvariant();
            if (stopAnswer == "y" || stopAnswer == "yes" || stopAnswer == "д" || stopAnswer == "да")
            {
                manager.StopAll();
                Console.WriteLine("Серверы остановлены.");
            }
            return true;
        }

        private static void WaitKey()
        {
            Console.WriteLine("\nНажмите Enter...");
            Console.ReadLine();
        }

        private static void ClearScreen() { try { Console.Clear(); } catch { } }

        private static string Truncate(string value, int length)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (value.Length <= length) return value;
            return value.Substring(0, length);
        }
    }
}
