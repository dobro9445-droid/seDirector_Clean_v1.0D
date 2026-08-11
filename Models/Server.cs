using System.Text.Json.Serialization;

namespace seDirector.Models;

public sealed class Server
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = "Unnamed server";

    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("Arguments")]
    public string Arguments { get; set; } = string.Empty;

    [JsonPropertyName("AutoStart")]
    public bool AutoStart { get; set; } = false;

    [JsonPropertyName("RestartOnExit")]
    public bool RestartOnExit { get; set; } = false;
}
