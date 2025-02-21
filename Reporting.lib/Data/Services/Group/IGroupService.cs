
namespace Reporting.lib.Data.Services.Group;

public interface IGroupService
{
    Task<IEnumerable<EmailGroup>> GetAllGroups();
    Task<int> AddGroup(string group, string rdpIp);
    Task<bool> DeleteGroup(int groupId);
}