using System.IO.Compression;
using seDirector.Models;

namespace seDirector.Core;

public sealed class BackupService
{
    private readonly Logger _logger;
    private readonly string _defaultBackupRoot;

    public BackupService(string baseDirectory, Logger logger)
    {
        _logger = logger;
        _defaultBackupRoot = Path.Combine(baseDirectory, "Backups");
    }

    public bool BackupServer(Server server)
    {
        if (server == null)
            return false;

        var backup = server.Backup;

        if (backup == null || string.IsNullOrWhiteSpace(backup.Source))
        {
            _logger.Warning("Сервер '" + server.Name + "': резервное копирование не настроено.");
            return false;
        }

        var destinationRoot = string.IsNullOrWhiteSpace(backup.Destination)
            ? Path.Combine(_defaultBackupRoot, GetSafeFileName(server.Name))
            : backup.Destination;

        return BackupPath(
            backup.Source,
            destinationRoot,
            server.Name,
            backup.MaxCopies
        );
    }

    public int BackupAll(List<Server> servers)
    {
        var successCount = 0;

        foreach (var server in servers)
        {
            if (server == null)
                continue;

            var backup = server.Backup;

            if (backup == null)
                continue;

            if (!backup.Enabled)
                continue;

            if (string.IsNullOrWhiteSpace(backup.Source))
                continue;

            if (BackupServer(server))
                successCount++;
        }

        return successCount;
    }

    private bool BackupPath(string source, string destinationRoot, string serverName, int maxCopies)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                _logger.Warning("Сервер '" + serverName + "': не указан источник резервного копирования.");
                return false;
            }

            if (maxCopies < 1)
                maxCopies = 1;

            Directory.CreateDirectory(destinationRoot);

            var safeName = GetSafeFileName(serverName);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var zipPath = Path.Combine(destinationRoot, safeName + "_backup_" + timestamp + ".zip");

            if (Directory.Exists(source))
            {
                ZipFile.CreateFromDirectory(
                    source,
                    zipPath,
                    CompressionLevel.Optimal,
                    false
                );
            }
            else if (File.Exists(source))
            {
                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(
                        source,
                        Path.GetFileName(source),
                        CompressionLevel.Optimal
                    );
                }
            }
            else
            {
                _logger.Error("Сервер '" + serverName + "': источник резервной копии не найден: " + source);
                return false;
            }

            _logger.Info("Резервная копия создана: " + zipPath);

            CleanupOldBackups(destinationRoot, safeName, maxCopies);

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Сервер '" + serverName + "': ошибка резервного копирования: " + ex.Message);
            return false;
        }
    }

    private void CleanupOldBackups(string destinationRoot, string safeName, int maxCopies)
    {
        try
        {
            var pattern = safeName + "_backup_*.zip";

            var files = Directory.GetFiles(destinationRoot, pattern)
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.CreationTime)
                .ToList();

            for (var i = maxCopies; i < files.Count; i++)
            {
                files[i].Delete();
                _logger.Info("Удалена старая резервная копия: " + files[i].FullName);
            }
        }
        catch
        {
            // Очистка старых копий не должна прерывать резервное копирование.
        }
    }

    private static string GetSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "server";

        var invalidChars = Path.GetInvalidFileNameChars();

        var chars = name
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray();

        return new string(chars);
    }
}
