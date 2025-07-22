
using ReportingApi.Models;

namespace Reporting.lib.Data.Services.Group;

public interface IGroupService
{
    Task<IEnumerable<EmailGroup>> GetAllGroups();
    Task<int> AddGroup(string group, string rdpIp);
    Task<bool> DeleteGroup(int groupId);
    // Remove the single group method, use the multi-group for everything
    Task<MoveEmailsResult> MoveEmailsBetweenMultipleGroups(string sourceGroupNames, string destinationGroupName);
}