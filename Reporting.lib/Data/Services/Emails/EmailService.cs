using System.Data;
using Dapper;
using Reporting.lib.enums.Core;
using Reporting.lib.Models.DTO;

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
        var result = await _dbConnection.LoadDataWithMappingAsync<
            EmailAccount, Proxy, EmailGroup, EmailMetaData, EmailAccountStats, dynamic>(
            "[dbo].[GetAllEmails]",
            new { }, // Replace with your actual parameters
            (email, ProxyModel, group, emailMetaData, stats) =>
            {
                email.Proxy = ProxyModel;
                email.Group = group;
                email.MetaIds = emailMetaData;
                email.Stats = stats;
                return email;
            },
            "ProxyId,GroupId,MetaDataId,InboxCount"
        ); 
        return result;
    }

public async Task AddEmailsToGroupWithMetadataAsync(
    IEnumerable<CreateEmailAccountDto> emails,
    Dictionary<string, EmailMetadataDto> emailMetadata,
    int? groupId = null,
    string? groupName = null)
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
    emailTable.Columns.Add("MailId", typeof(string));
    emailTable.Columns.Add("YmreqId", typeof(string));
    emailTable.Columns.Add("Wssid", typeof(string));
    emailTable.Columns.Add("Cookie", typeof(string));

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
            proxy?.Password,
            emailMetadata[email.EmailAddress].MailId,
            emailMetadata[email.EmailAddress].YmreqId,
            emailMetadata[email.EmailAddress].Wssid,
            emailMetadata[email.EmailAddress].Cookie
        );
    }

    var parameters = new
    {
        EmailsWithMetadata = emailTable.AsTableValuedParameter("EmailTableWithMetadataType"),
        GroupId = groupId,
        GroupName = groupName
    };
    // Retrieve inserted emails
    var insertedEmails = await _dbConnection.SaveDataAsync<(int EmailAccountId, string EmailAddress)>(
        "[dbo].[AddEmailsToGroupWithProxy_Create]",
        parameters
    );
    
}


    public async Task<IEnumerable<EmailAccount>> GetEmailsByGroupAsync(int groupId)
    {
        var results = await _dbConnection.LoadDataAsync<EmailAccount, dynamic>("[dbo].[GetEmailsByGroup]", new
        {
            GroupId = groupId
        });

        return results;
    }

    public async Task UpdateEmailStatsBatchAsync(IEnumerable<EmailStatsUpdateDto> emailAccounts)
    {
        var table = new DataTable();
        table.Columns.Add("EmailAccountId", typeof(int));
        table.Columns.Add("InboxCount", typeof(int));
        table.Columns.Add("SpamCount", typeof(int));
        table.Columns.Add("LastReadCount", typeof(int));
        table.Columns.Add("LastArchivedCount", typeof(int));
        table.Columns.Add("LastNotSpamCount", typeof(int));
        table.Columns.Add("UpdatedAt", typeof(DateTime));

        foreach (var email in emailAccounts)
        {
            table.Rows.Add(
                email.EmailAccountId,
                email.InboxCount,
                email.SpamCount,
                email.LastReadCount,
                email.LastArchivedCount,
                email.LastNotSpamCount,
                DateTime.UtcNow
            );
        }

        var parameters = new { EmailStats = table.AsTableValuedParameter("EmailStatsType") };

        await _dbConnection.SaveDataAsync(
            "[dbo].[UpdateEmailStatsBatch]",
            parameters
        );
    }
}