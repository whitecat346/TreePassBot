namespace TreePassBot.Services.Interfaces;

public interface IAuditService
{
    Task<bool> ProcessApprovalAsync(ulong targetQqId, ulong operatorQqId, ulong groupId);
    Task<bool> ProcessDenialAsync(ulong targetId, ulong operatorQqId, ulong groupId);
}