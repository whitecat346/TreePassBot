using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreePassBot.Services.Interfaces;

public interface IAuditService
{
    Task ProcessApprovalAsync(ulong targetQqId, ulong operatorQqId);
    Task ProcessDenialAsync(ulong targetQqId, ulong operatorQqId);
}