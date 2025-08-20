namespace TreePassBot.Data.Entities;

public enum AuditStatus
{
    Pending,
    Suspend,
    Dying,
    Approved,
    Expired,
    Denied
}

public record UserInfo
{
    public ulong QqId { get; init; }
    public AuditStatus Status { get; set; } = AuditStatus.Pending;

    public string Passcode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpriedAt { get; set; }
}