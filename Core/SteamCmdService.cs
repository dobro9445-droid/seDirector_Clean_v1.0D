using System.Diagnostics;
using System.Threading.Tasks;
using seDirector.Models;

namespace seDirector.Core;

public sealed class SteamCmdService
{
    private readonly string _steamCmdPath;
    private readonly Logger _logger;

    public SteamCmdService(string baseDirectory, Logger logger)
    {
        _logger = logger;
        _steamCmdPath = Path.Combine(baseDirectory, "steamcmd", "steamcmd.exe");
    }

    public bool IsAvailable()
    {
        return File.Exists(_steamCmdPath);
    }

    public bool UpdateServer(Server server)
    {
        if (server == null)
            return false;

        if (string.IsNullOrWhiteSpace(server.SteamAppId))
        {
            _logger.Warning("Сервер '" + server.Name + "': не указан SteamAppId.");
            return false;
        }

        if (!IsAvailable())
        {
            _logger.Error("SteamCMD не найден. Ожидается файл: " + _steamCmdPath);
            return false;
        }

        var installDir = Path.GetDirectoryName(server.Path);

        if (string.IsNullOrWhiteSpace(installDir))
        {
            _logger.Warning("Сервер '" + server.Name + "': не удалось определить папку установки.");
            return false;
        }

        var arguments =
            "+login anonymous " +
            "+force_install_dir \"" + installDir + "\" " +
            "+app_update " + server.SteamAppId + " validate " +
            "+quit";

        _logger.Info("SteamCMD: обновление сервера '" + server.Name + "' (App ID " + server.SteamAppId + ")...");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _steamCmdPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;

            if (!process.Start())
            {
                _logger.Error("SteamCMD: не удалось запустить процесс.");
                return false;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            var output = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.Info("SteamCMD вывод: " + output.Trim());
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.Warning("SteamCMD ошибки: " + error.Trim());
            }

            if (process.ExitCode == 0)
            {
                _logger.Info("SteamCMD: сервер '" + server.Name + "' успешно обновлён.");
                return true;
            }

            _logger.Error("SteamCMD: обновление завершилось с кодом " + process.ExitCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error("SteamCMD: ошибка обновления сервера '" + server.Name + "': " + ex.Message);
            return false;
        }
    }
}
