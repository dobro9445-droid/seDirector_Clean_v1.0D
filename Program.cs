using System.Windows.Forms;
using seDirector.Core;
using seDirector.GUI;

namespace seDirector;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

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

        logger.Info("Приложение seDirector Clean v1.5 запущено (GUI).");

        Application.Run(new MainForm(manager, backupService, rconService, steamCmdService, softStopService, updateService, logger));

        logger.Info("Приложение seDirector Clean v1.5 завершено.");
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
}
