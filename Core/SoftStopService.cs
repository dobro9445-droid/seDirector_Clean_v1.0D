using System;
using System.Threading;
using seDirector.Models;

namespace seDirector.Core;

public sealed class SoftStopService
{
    private readonly ServerManager _manager;
    private readonly RconService _rcon;
    private readonly Logger _logger;

    public SoftStopService(ServerManager manager, RconService rcon, Logger logger)
    {
        _manager = manager;
        _rcon = rcon;
        _logger = logger;
    }

    public bool Stop(int index)
    {
        if (!_manager.IsValidIndex(index))
            return false;

        var server = _manager.Servers[index];

        if (!_manager.IsRunning(index))
        {
            _logger.Warning("Сервер '" + server.Name + "' не запущен.");
            return false;
        }

        _logger.Info("Мягкая остановка сервера '" + server.Name + "'...");

        if (server.Rcon != null && server.Rcon.Enabled)
        {
            var commands = GetStopCommands(server.Type);

            foreach (var command in commands)
            {
                _logger.Info("Попытка мягкой остановки командой: " + command);

                string response;
                _rcon.SendCommand(server, command, out response);

                if (WaitForExit(index, 10))
                {
                    _logger.Info("Сервер '" + server.Name + "' мягко остановлен.");

                    // Отмечаем остановку как ручную, чтобы автоперезапуск не запустил сервер снова.
                    _manager.TryStop(index);

                    return true;
                }
            }
        }
        else
        {
            _logger.Info("RCON недоступен, будет использована обычная остановка процесса.");
        }

        return _manager.TryStop(index);
    }

    private static string[] GetStopCommands(string type)
    {
        var t = (type ?? string.Empty).Trim().ToLowerInvariant();

        if (t == "minecraft")
        {
            return new string[]
            {
                "stop"
            };
        }

        if (t == "rust")
        {
            return new string[]
            {
                "server.stop"
            };
        }

        return new string[]
        {
            "quit",
            "exit",
            "stop"
        };
    }

    private bool WaitForExit(int index, int seconds)
    {
        for (var i = 0; i < seconds; i++)
        {
            if (!_manager.IsRunning(index))
                return true;

            Thread.Sleep(1000);
        }

        return !_manager.IsRunning(index);
    }
}
