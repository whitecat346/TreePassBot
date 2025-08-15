using System.Text.Json.Serialization;

namespace TreePassBot.Models;

public class BotConfig
{
    [JsonPropertyName("BotQqId")]
    public ulong BotQqId { get; init; }

    [JsonPropertyName("AuditGroupId")]
    public ulong AuditGroupId { get; init; }

    [JsonPropertyName("MainGroupIds")]
    public List<ulong> MainGroupIds { get; init; } = [];

    [JsonPropertyName("AuditorQqIds")]
    public List<ulong> AuditorQqIds { get; init; } = [];

    [JsonPropertyName("AdminQqIds")]
    public List<ulong> AdminQqIds { get; init; } = [];

    [JsonPropertyName("DataFile")]
    public string DataFile { get; init; } = "bot_data.json";
}