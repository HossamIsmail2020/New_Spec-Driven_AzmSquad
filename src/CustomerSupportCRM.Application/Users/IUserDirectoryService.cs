namespace CustomerSupportCRM.Application.Users;

/// <summary>Read-only agent directory for the assignment picker (plan.md §5). Deliberately minimal -- full user management is bonus scope.</summary>
public interface IUserDirectoryService
{
    Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken ct = default);
}
