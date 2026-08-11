using System.Net.Sockets;
using System.Text;
using seDirector.Models;

namespace seDirector.Core;

public sealed class RconService
{
    private const int TimeoutMs = 8000;

    private readonly Logger _logger;

    public RconService(Logger logger)
    {
        _logger = logger;
    }

    public bool SendCommand(Server server, string command, out string response)
    {
        response = string.Empty;

        if (server == null)
            return false;

        var rcon = server.Rcon;

        if (rcon == null)
        {
            _logger.Warning("Сервер '" + server.Name + "': RCON не настроен.");
            return false;
        }

        if (!rcon.Enabled)
        {
            _logger.Warning("Сервер '" + server.Name + "': RCON отключён в конфигурации.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(rcon.Host) || rcon.Port <= 0)
        {
            _logger.Warning("Сервер '" + server.Name + "': неверный адрес или порт RCON.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(rcon.Password))
        {
            _logger.Warning("Сервер '" + server.Name + "': не задан пароль RCON.");
            return false;
        }

        try
        {
            using var client = new TcpClient();

            var connectTask = client.ConnectAsync(rcon.Host, rcon.Port);

            if (!connectTask.Wait(TimeoutMs))
            {
                _logger.Error("RCON: тайм-аут подключения к " + rcon.Host + ":" + rcon.Port);
                return false;
            }

            client.ReceiveTimeout = TimeoutMs;
            client.SendTimeout = TimeoutMs;

            var stream = client.GetStream();

            if (!Authenticate(stream, rcon.Password, server.Name))
                return false;

            SendPacket(stream, 2, 2, command);

            if (TryReadCommandResponse(stream, 2, out response))
            {
                _logger.Info("RCON: команда выполнена для сервера '" + server.Name + "'.");
                return true;
            }

            _logger.Warning("RCON: не получен ответ от сервера '" + server.Name + "'.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error("RCON: ошибка для сервера '" + server.Name + "': " + ex.Message);
            return false;
        }
    }

    private bool Authenticate(NetworkStream stream, string password, string serverName)
    {
        SendPacket(stream, 1, 3, password);

        for (var i = 0; i < 3; i++)
        {
            int id;
            int type;
            string body;

            if (!TryReadPacket(stream, out id, out type, out body))
            {
                _logger.Error("RCON: не получен ответ аутентификации от сервера '" + serverName + "'.");
                return false;
            }

            if (id == -1)
            {
                _logger.Error("RCON: неверный пароль для сервера '" + serverName + "'.");
                return false;
            }

            if (id == 1)
            {
                _logger.Info("RCON: аутентификация успешна для сервера '" + serverName + "'.");
                return true;
            }
        }

        _logger.Error("RCON: аутентификация не завершена для сервера '" + serverName + "'.");
        return false;
    }

    private bool TryReadCommandResponse(NetworkStream stream, int expectedId, out string response)
    {
        response = string.Empty;

        var builder = new StringBuilder();

        for (var i = 0; i < 5; i++)
        {
            int id;
            int type;
            string body;

            if (!TryReadPacket(stream, out id, out type, out body))
            {
                response = builder.ToString();
                return response.Length > 0;
            }

            if (id == expectedId)
            {
                builder.Append(body);
                response = builder.ToString();
                return true;
            }

            if (!string.IsNullOrEmpty(body))
                builder.Append(body);
        }

        response = builder.ToString();
        return response.Length > 0;
    }

    private static void SendPacket(NetworkStream stream, int id, int type, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
        var packet = new byte[bodyBytes.Length + 14];
        var length = bodyBytes.Length + 10;

        BitConverter.GetBytes(length).CopyTo(packet, 0);
        BitConverter.GetBytes(id).CopyTo(packet, 4);
        BitConverter.GetBytes(type).CopyTo(packet, 8);
        bodyBytes.CopyTo(packet, 12);

        packet[packet.Length - 2] = 0;
        packet[packet.Length - 1] = 0;

        stream.Write(packet, 0, packet.Length);
    }

    private static bool TryReadPacket(NetworkStream stream, out int id, out int type, out string body)
    {
        id = 0;
        type = 0;
        body = string.Empty;

        var lengthBuffer = new byte[4];

        if (!TryReadExact(stream, lengthBuffer, 0, 4))
            return false;

        var length = BitConverter.ToInt32(lengthBuffer, 0);

        if (length < 10 || length > 4096000)
            return false;

        var payload = new byte[length];

        if (!TryReadExact(stream, payload, 0, length))
            return false;

        id = BitConverter.ToInt32(payload, 0);
        type = BitConverter.ToInt32(payload, 4);

        var bodyLength = length - 10;

        if (bodyLength > 0)
            body = Encoding.UTF8.GetString(payload, 8, bodyLength);

        return true;
    }

    private static bool TryReadExact(NetworkStream stream, byte[] buffer, int offset, int count)
    {
        var total = 0;

        while (total < count)
        {
            var read = stream.Read(buffer, offset + total, count - total);

            if (read <= 0)
                return false;

            total += read;
        }

        return true;
    }
}
