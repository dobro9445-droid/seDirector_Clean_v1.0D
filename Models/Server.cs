using System.Text.Json.Serialization;

namespace seDirector.Models;

public sealed class Server
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = "Unnamed server";

    // Тип сервера: Generic, Source, Minecraft, Rust, CS2, SteamCMD и т.д.
    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Generic";

    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("Arguments")]
    public string Arguments { get; set; } = string.Empty;

    // Порт сервера, если нужна проверка порта
    [JsonPropertyName("Port")]
    public int? Port { get; set; }

    // Приоритет процесса: Low, Normal, High
    [JsonPropertyName("Priority")]
    public string Priority { get; set; } = "Normal";

    // Запускать сервер вместе с программой
    [JsonPropertyName("AutoStart")]
    public bool AutoStart { get; set; } = false;

    // Перезапускать сервер, если он неожиданно завершился
    [JsonPropertyName("RestartOnExit")]
    public bool RestartOnExit { get; set; } = false;

    // Задержка перед автоперезапуском в секундах
    [JsonPropertyName("RestartDelaySeconds")]
    public int RestartDelaySeconds { get; set; } = 10;

    // Максимальное число попыток автоперезапуска подряд
    [JsonPropertyName("MaxRestartAttempts")]
    public int MaxRestartAttempts { get; set; } = 3;

    // Обновлять сервер через SteamCMD перед запуском
    [JsonPropertyName("UpdateBeforeStart")]
    public bool UpdateBeforeStart { get; set; } = false;

    // Steam App ID для SteamCMD
    [JsonPropertyName("SteamAppId")]
    public string? SteamAppId { get; set; }

    // Настройки RCON
    [JsonPropertyName("Rcon")]
    public RconConfig? Rcon { get; set; }

    // Настройки резервного копирования
    [JsonPropertyName("Backup")]
    public BackupConfig? Backup { get; set; }

    // Расписание задач
    [JsonPropertyName("Schedule")]
    public ScheduleConfig? Schedule { get; set; }
}

public sealed class RconConfig
{
    [JsonPropertyName("Host")]
    public string Host { get; set; } = "127.0.0.1";

    [JsonPropertyName("Port")]
    public int Port { get; set; } = 27015;

    [JsonPropertyName("Password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;
}

public sealed class BackupConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("Source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("Destination")]
    public string Destination { get; set; } = string.Empty;

    // Сколько последних резервных копий хранить
    [JsonPropertyName("MaxCopies")]
    public int MaxCopies { get; set; } = 5;

    // Создавать копию перед запуском сервера
    [JsonPropertyName("BackupBeforeStart")]
    public bool BackupBeforeStart { get; set; } = false;

    // Создавать копию перед остановкой сервера
    [JsonPropertyName("BackupBeforeStop")]
    public bool BackupBeforeStop { get; set; } = false;
}

public sealed class ScheduleConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;

    // Время запуска сервера, например 08:00
    [JsonPropertyName("Start")]
    public string? Start { get; set; }

    // Время остановки сервера, например 23:00
    [JsonPropertyName("Stop")]
    public string? Stop { get; set; }

    // Время перезапуска сервера, например 04:00
    [JsonPropertyName("Restart")]
    public string? Restart { get; set; }

    // Время резервного копирования, например 03:30
    [JsonPropertyName("Backup")]
    public string? Backup { get; set; }
}
