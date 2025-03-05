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
            EmailAccount, Models.Core.Proxy, EmailGroup, EmailMetaData, EmailAccountStats, dynamic>(
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
    public async Task<IEnumerable<RetrieveFailedEmailDto>> GetFailedEmailsByGroupAsync(int groupId)
    {
        var results = await _dbConnection.LoadDataAsync<RetrieveFailedEmailDto, dynamic>("[dbo].[GetFailedEmailsByGroup]", new
        {
            GroupId = groupId
        });

        return results;
    }
    public async Task DeleteBannedEmailsAsync(IEnumerable<string> bannedEmails)
    {
        var table = new DataTable();
        table.Columns.Add("EmailAddress", typeof(string));

        foreach (var email in bannedEmails)
        {
            table.Rows.Add(email);
        }

        var parameters = new { BannedEmails = table.AsTableValuedParameter("BannedEmailType") };

        await _dbConnection.SaveDataAsync("[dbo].[DeleteBannedEmails]", parameters);
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
    public async Task UpdateEmailMetadataBatchAsync(IEnumerable<EmailMetadata> metadataList)
    {
        var table = new DataTable();
        table.Columns.Add("EmailAccountId", typeof(int));
        table.Columns.Add("MailId", typeof(string));
        table.Columns.Add("YmreqId", typeof(string));
        table.Columns.Add("Wssid", typeof(string));
        table.Columns.Add("Cookie", typeof(string));

        foreach (var metadata in metadataList)
        {
            table.Rows.Add(
                metadata.EmailAccountId,
                metadata.MailId,
                metadata.YmreqId,
                metadata.Wssid,
                metadata.Cookie
            );
        }

        var parameters = new { EmailMetadataList = table.AsTableValuedParameter("EmailMetadataType") };

        await _dbConnection.SaveDataAsync(
            "[dbo].[UpdateEmailMetadata]",
            parameters
        );
    }


    public async Task UpdateEmailProxiesBatchAsync(IEnumerable<EmailProxyMappingDto> emailProxyMappings)
    {
        var table = new DataTable();
        table.Columns.Add("EmailAddress", typeof(string));
        table.Columns.Add("ProxyIp", typeof(string));
        table.Columns.Add("Port", typeof(int));

        foreach (var mapping in emailProxyMappings)
        {
            table.Rows.Add(
                mapping.EmailAddress,
                mapping.ProxyIp,
                mapping.Port
            );
        }

        var parameters = new { EmailProxyMappings = table.AsTableValuedParameter("EmailProxyMappingType") };

        await _dbConnection.SaveDataAsync(
            "[dbo].[UpdateEmailProxyBatch]",
            parameters
        );
    }
    
    public async Task AddFailedEmailsBatchAsync(IEnumerable<FailedEmailDto> failedEmails)
    {
        var table = new DataTable();
        table.Columns.Add("EmailId", typeof(int));
        table.Columns.Add("FailureReason", typeof(string));

        foreach (var email in failedEmails)
        {
            table.Rows.Add(email.EmailId, email.FailureReason);
        }

        var parameters = new { FailedEmails = table.AsTableValuedParameter("FailedEmailTableType") };

        await _dbConnection.SaveDataAsync("[dbo].[AddFailedEmailsBatch]", parameters);
    }

    public async Task<int> DeleteEmailsAsync(string emailText)
    {
        if (string.IsNullOrWhiteSpace(emailText))
            throw new Exception("Email text cannot be empty");

        // Convert the multi-line TextBox input into a list of email addresses
        var emailList = emailText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(email => new { EmailAddress = email.Trim() })
            .ToList();

        if (!emailList.Any())
            throw new Exception("Email text cannot be empty");

        // Create a DataTable for the table-valued parameter
        var table = new DataTable();
        table.Columns.Add("EmailAddress", typeof(string));

        foreach (var email in emailList)
        {
            table.Rows.Add(email.EmailAddress);
        }

        var parameters = new { EmailList = table.AsTableValuedParameter("EmailListType") };

        // Call SaveDataAsync and retrieve the deleted count
        int deletedCount = await _dbConnection.SaveDataAsync("[dbo].[DeleteEmailAccountsBatch]", parameters);

        return deletedCount; // Return the number of deleted emails
    }



}