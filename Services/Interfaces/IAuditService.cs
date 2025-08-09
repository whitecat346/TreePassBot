namespace TreePassBot.Services.Interfaces;

public interface IAuditService
{
    Task<bool> ProcessApprovalAsync(ulong targetQqId, ulong operatorQqId, ulong groupId);
    Task ProcessDenialAsync(ulong targetQqId, ulong operatorQqId);
}