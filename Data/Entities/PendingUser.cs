namespace TreePassBot.Data.Entities;

public enum AuditStatus
{
    Pending,
    Suspend,
    Dying,
    Approved,
    Expried,
    Denied
}

public record PendingUser
{
    public ulong QqId { get; set; }
    public AuditStatus Status { get; set; } = AuditStatus.Pending;

    public string Passcode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ExpriedAt { get; set; }
}