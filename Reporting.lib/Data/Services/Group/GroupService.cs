using Reporting.lib.enums.Core;
using ReportingApi.Models;

namespace Reporting.lib.Data.Services.Group;

   public class GroupService : IGroupService
{
    private readonly IDataConnection _dataConnection;

    public GroupService(IDataConnection dataConnection)
    {
        _dataConnection = dataConnection;
    }

    public async Task<IEnumerable<EmailGroup>> GetAllGroups()
    {
        return await _dataConnection.LoadDataAsync<EmailGroup, dynamic>("dbo.GetGroups", new { });
    }

    public async Task<int> AddGroup(string group, string rdpIp)
    {
        return await _dataConnection.SaveDataAsync("[dbo].[AddGroup]", new { Name = group, RdpIp = rdpIp });
    }

    public async Task<bool> DeleteGroup(int groupId)
    {
        try
        {
            await _dataConnection.SaveDataAsync("DeleteGroupAndEmails", new { GroupId = groupId });
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    // Single method handles both single and multi-group moves
    public async Task<MoveEmailsResult> MoveEmailsBetweenMultipleGroups(string sourceGroupNames, string destinationGroupName)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Service: Moving from '{sourceGroupNames}' to '{destinationGroupName}'");
            
            var result = await _dataConnection.LoadDataAsync<MoveEmailsResult, dynamic>(
                "dbo.MoveEmailsBetweenMultipleGroups", 
                new { 
                    SourceGroupNames = sourceGroupNames, 
                    DestinationGroupName = destinationGroupName 
                });

            var moveResult = result.FirstOrDefault();
            if (moveResult != null)
            {
                System.Diagnostics.Debug.WriteLine($"Service Result: Status={moveResult.Status}, EmailsMovedCount={moveResult.EmailsMovedCount}, Message={moveResult.Message}");
                return moveResult;
            }

            return new MoveEmailsResult
            {
                Status = "ERROR",
                Message = "No result returned from stored procedure",
                EmailsMovedCount = 0,
                SourceGroups = sourceGroupNames,
                DestinationGroup = destinationGroupName,
                NotFoundGroups = ""
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Service Exception: {ex.Message}");
            return new MoveEmailsResult
            {
                Status = "ERROR",
                Message = $"Exception occurred: {ex.Message}",
                EmailsMovedCount = 0,
                SourceGroups = sourceGroupNames,
                DestinationGroup = destinationGroupName,
                NotFoundGroups = ""
            };
        }
    }
}