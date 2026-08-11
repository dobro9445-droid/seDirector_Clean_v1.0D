using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace seDirector.Core;

public sealed class WebPanelService : IDisposable
{
    private readonly ServerManager _manager;
    private readonly BackupService _backupService;
    private readonly Logger _logger;
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts;
    private readonly Task _listenTask;
    private readonly int _port;

    public WebPanelService(ServerManager manager, BackupService backupService, Logger logger, int port = 8080)
    {
        _manager = manager;
        _backupService = backupService;
        _logger = logger;
        _port = port;

        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:" + _port + "/");
        _listener.Prefixes.Add("http://127.0.0.1:" + _port + "/");

        _cts = new CancellationTokenSource();

        try
        {
            _listener.Start();
            _listenTask = Task.Run(() => ListenLoop(_cts.Token));
            _logger.Info("Локальная веб-панель запущена на http://localhost:" + _port + "/");
        }
        catch (Exception ex)
        {
            _logger.Error("Не удалось запустить веб-панель: " + ex.Message);
            throw;
        }
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("WebPanel: " + ex.Message);
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url.AbsolutePath.ToLowerInvariant();
            var method = context.Request.HttpMethod.ToUpperInvariant();

            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Cache-Control", "no-cache");

            if (path == "/" || path == "/index.html")
            {
                ServeHtml(context);
            }
            else if (path == "/api/servers" && method == "GET")
            {
                ServeJsonServers(context);
            }
            else if (path.StartsWith("/api/start/") && method == "POST")
            {
                HandleAction(context, path, _manager.TryStart);
            }
            else if (path.StartsWith("/api/stop/") && method == "POST")
            {
                HandleAction(context, path, _manager.TryStop);
            }
            else if (path.StartsWith("/api/restart/") && method == "POST")
            {
                HandleAction(context, path, _manager.TryRestart);
            }
            else if (path.StartsWith("/api/backup/") && method == "POST")
            {
                HandleBackup(context, path);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            _logger.Error("WebPanel HandleRequest: " + ex.Message);
            context.Response.StatusCode = 500;
        }
        finally
        {
            try { context.Response.Close(); } catch { }
        }
    }

    private void ServeHtml(HttpListenerContext context)
    {
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>seDirector Clean v1.4</title>
    <style>
        body { font-family: sans-serif; background: #1e1e1e; color: #eee; padding: 20px; }
        h1 { color: #4CAF50; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th, td { border: 1px solid #444; padding: 10px; text-align: left; }
        th { background: #333; }
        .btn { padding: 5px 10px; margin: 2px; cursor: pointer; border: none; border-radius: 4px; color: white; }
        .start { background: #4CAF50; }
        .stop { background: #f44336; }
        .restart { background: #ff9800; }
        .backup { background: #2196F3; }
        .status-running { color: #4CAF50; font-weight: bold; }
        .status-stopped { color: #f44336; font-weight: bold; }
    </style>
</head>
<body>
    <h1>seDirector Clean v1.4</h1>
    <p>Локальная панель управления</p>
    <button onclick='loadServers()'>Обновить</button>
    <div id='content'></div>

    <script>
        async function loadServers() {
            const res = await fetch('/api/servers');
            const servers = await res.json();
            let html = '<table><tr><th>#</th><th>Имя</th><th>Статус</th><th>Аптайм</th><th>Память</th><th>Действия</th></tr>';
            servers.forEach((s, i) => {
                const statusClass = s.status.includes('RUNNING') ? 'status-running' : 'status-stopped';
                html += `<tr>
                    <td>${i + 1}</td>
                    <td>${s.name}</td>
                    <td class='${statusClass}'>${s.status}</td>
                    <td>${s.uptime}</td>
                    <td>${s.memory}</td>
                    <td>
                        <button class='btn start' onclick='act(${i}, ""start"")'>Start</button>
                        <button class='btn stop' onclick='act(${i}, ""stop"")'>Stop</button>
                        <button class='btn restart' onclick='act(${i}, ""restart"")'>Restart</button>
                        <button class='btn backup' onclick='act(${i}, ""backup"")'>Backup</button>
                    </td>
                </tr>`;
            });
            html += '</table>';
            document.getElementById('content').innerHTML = html;
        }

        async function act(index, action) {
            await fetch(`/api/${action}/${index}`, { method: 'POST' });
            setTimeout(loadServers, 500);
        }

        setInterval(loadServers, 5000);
        loadServers();
    </script>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private void ServeJsonServers(HttpListenerContext context)
    {
        var list = new List<object>();
        for (var i = 0; i < _manager.Servers.Count; i++)
        {
            var s = _manager.Servers[i];
            list.Add(new
            {
                name = s.Name,
                type = s.Type,
                path = s.Path,
                status = _manager.GetStatus(i),
                uptime = _manager.GetUptime(i),
                memory = _manager.GetMemoryUsage(i),
                port = s.Port
            });
        }

        var json = JsonSerializer.Serialize(list);
        var buffer = Encoding.UTF8.GetBytes(json);
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private void HandleAction(HttpListenerContext context, string path, Func<int, bool> action)
    {
        var parts = path.Split('/');
        if (parts.Length >= 4 && int.TryParse(parts[3], out var index))
        {
            var success = action(index);
            var json = JsonSerializer.Serialize(new { success });
            var buffer = Encoding.UTF8.GetBytes(json);
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }

    private void HandleBackup(HttpListenerContext context, string path)
    {
        var parts = path.Split('/');
        if (parts.Length >= 4 && int.TryParse(parts[3], out var index))
        {
            if (_manager.IsValidIndex(index))
            {
                var success = _backupService.BackupServer(_manager.Servers[index]);
                var json = JsonSerializer.Serialize(new { success });
                var buffer = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
            _listenTask.Wait(2000);
        }
        catch { }

        _logger.Info("Локальная веб-панель остановлена.");
    }
}
