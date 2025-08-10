namespace TreePassBot.Models;

public class BotConfig
{
    public ulong BotQqId { get; init; }
    public ulong AuditGroupId { get; init; }
    public List<ulong> MainGroupId { get; init; } = [];
    public List<ulong> AuditorQqIds { get; init; } = [];
    public string QuestionnaireLink { get; init; } = "Undefined";

    public ulong AdminQqId { get; init; }

    public string DataFile { get; init; } = "bot_data.json";
}