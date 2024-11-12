using Reporting.lib.enums.Core;

namespace Reporting.lib.Data.Services.Group;

public class GroupService : IGroupService
{
    private readonly IDataConnection _dataConnection;

    public GroupService(IDataConnection dataConnection)
    {
        _dataConnection = dataConnection;
    }


    public async  Task<IEnumerable<EmailGroup>> GetAllGroups()
    {
        return await _dataConnection.LoadDataAsync<EmailGroup, dynamic>("dbo.GetGroups",new {});
    }

    public async Task<int> AddGroup(EmailGroup group)
    {
        return await _dataConnection.SaveDataAsync("[dbo].[AddGroup]", new {Name  = group});
    }
}