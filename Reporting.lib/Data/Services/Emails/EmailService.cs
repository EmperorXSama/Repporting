using System.Data;
using Dapper;
using Reporting.lib.enums.Core;

namespace Reporting.lib.Data.Services.Emails;

public class EmailService : IEmailService
{
    private readonly IDataConnection _dbConnection;
    public EmailService(IDataConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IEnumerable<EmailAccount>> GetAllEmailsAsync()
    {
        var result = await _dbConnection.LoadDataWithMappingAsync<EmailAccount, Proxy,EmailGroup, dynamic>(
            "[dbo].[GetAllEmails]",
            new { }, // Replace with your actual parameters
            (email, proxy,group) =>
            {
                email.Proxy = proxy;
                email.Group = group;
                return email;
            },
            "ProxyId,GroupId"
        );// Use the name of the column where the split should occur
        return result;
    }
    public async Task AddEmailsToGroupAsync(IEnumerable<EmailAccount> emails, int? groupId = null, string? groupName = null)
    {
        var emailTable = new DataTable();
        emailTable.Columns.Add("EmailAddress", typeof(string));
        emailTable.Columns.Add("Password", typeof(string));
        emailTable.Columns.Add("RecoveryEmail", typeof(string));
        emailTable.Columns.Add("Status", typeof(int));
        emailTable.Columns.Add("ProxyIp", typeof(string));
        emailTable.Columns.Add("Port", typeof(int));
        emailTable.Columns.Add("ProxyUsername", typeof(string));
        emailTable.Columns.Add("ProxyPassword", typeof(string));

        foreach (var email in emails)
        {
            var proxy = email.Proxy;
            emailTable.Rows.Add(
                email.EmailAddress, 
                email.Password, 
                email.RecoveryEmail, 
                (int)email.Status, 
                proxy?.ProxyIp, 
                proxy?.Port, 
                proxy?.Username, 
                proxy?.Password
            );
        }

        var parameters = new
        {
            Emails = emailTable.AsTableValuedParameter("EmailTableType"),
            GroupId = groupId,
            GroupName = groupName
        };

        await _dbConnection.SaveDataAsync("[dbo].[AddEmailsToGroup]", parameters);
    }
    public async Task<IEnumerable<EmailAccount>> GetEmailsByGroupAsync(int groupId)
    {
        var results = await _dbConnection.LoadDataAsync<EmailAccount, dynamic>("[dbo].[GetEmailsByGroup]", new
        {
            GroupId = groupId
        });

        return results;
    }

}