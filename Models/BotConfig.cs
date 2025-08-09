namespace TreePassBot.Models;

public class BotConfig
{
    public ulong BotQQId { get; set; }
    public ulong AuditGroupId { get; set; }
    public List<ulong> MainGroupId { get; set; } = [];
    public List<ulong> AuditorQQIds { get; set; } = [];
    public string QuestionnaireLink { get; set; }

    public string DataFile { get; set; } = "bot_data.json";
}