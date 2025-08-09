namespace TreePassBot.Models;

public class BotConfig
{
    public ulong BotQqId { get; set; }
    public ulong AuditGroupId { get; set; }
    public List<ulong> MainGroupId { get; set; } = [];
    public List<ulong> AuditorQqIds { get; set; } = [];
    public string QuestionnaireLink { get; set; } = "Undefined";

    public string DataFile { get; set; } = "bot_data.json";
}