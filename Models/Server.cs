using System.Text.Json.Serialization;

namespace seDirector.Models;

public sealed class Server
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = "Unnamed server";

    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Generic";

    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("Arguments")]
    public string Arguments { get; set; } = string.Empty;

    [JsonPropertyName("Port")]
    public int? Port { get; set; }

    [JsonPropertyName("Priority")]
    public string Priority { get; set; } = "Normal";

    [JsonPropertyName("AutoStart")]
    public bool AutoStart { get; set; } = false;

    [JsonPropertyName("RestartOnExit")]
    public bool RestartOnExit { get; set; } = false;

    [JsonPropertyName("RestartDelaySeconds")]
    public int RestartDelaySeconds { get; set; } = 10;

    [JsonPropertyName("MaxRestartAttempts")]
    public int MaxRestartAttempts { get; set; } = 3;

    [JsonPropertyName("UpdateBeforeStart")]
    public bool UpdateBeforeStart { get; set; } = false;

    [JsonPropertyName("SteamAppId")]
    public string? SteamAppId { get; set; }

    [JsonPropertyName("Rcon")]
    public RconConfig? Rcon { get; set; }

    [JsonPropertyName("Backup")]
    public BackupConfig? Backup { get; set; }

    [JsonPropertyName("Schedule")]
    public ScheduleConfig? Schedule { get; set; }

    [JsonPropertyName("Network")]
    public NetworkConfig? Network { get; set; }
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

    [JsonPropertyName("MaxCopies")]
    public int MaxCopies { get; set; } = 5;

    [JsonPropertyName("BackupBeforeStart")]
    public bool BackupBeforeStart { get; set; } = false;

    [JsonPropertyName("BackupBeforeStop")]
    public bool BackupBeforeStop { get; set; } = false;
}

public sealed class ScheduleConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("Start")]
    public string? Start { get; set; }

    [JsonPropertyName("Stop")]
    public string? Stop { get; set; }

    [JsonPropertyName("Restart")]
    public string? Restart { get; set; }

    [JsonPropertyName("Backup")]
    public string? Backup { get; set; }
}

public sealed class NetworkConfig
{
    [JsonPropertyName("LocalIP")]
    public string LocalIP { get; set; } = "127.0.0.1";

    [JsonPropertyName("ExternalIP")]
    public string ExternalIP { get; set; } = string.Empty;

    [JsonPropertyName("GamePort")]
    public int GamePort { get; set; } = 27015;

    [JsonPropertyName("RconPort")]
    public int RconPort { get; set; } = 27015;

    [JsonPropertyName("QueryPort")]
    public int QueryPort { get; set; } = 27015;

    [JsonPropertyName("UseWAN")]
    public bool UseWAN { get; set; } = false;

    [JsonPropertyName("WANPassword")]
    public string WANPassword { get; set; } = string.Empty;
}
