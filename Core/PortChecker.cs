using System.Net.Sockets;

namespace seDirector.Core;

public static class PortChecker
{
    public static bool IsPortOpen(string host, int port, int timeoutMs = 3000)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (port <= 0)
            return false;

        try
        {
            using var client = new TcpClient();

            var task = client.ConnectAsync(host, port);

            if (!task.Wait(timeoutMs))
                return false;

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
