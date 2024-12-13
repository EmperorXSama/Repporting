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
        var result = await _dbConnection.LoadDataWithMappingAsync<EmailAccount, Proxy,EmailGroup,EmailMetaData, dynamic>(
            "[dbo].[GetAllEmails]",
            new { }, // Replace with your actual parameters
            (email, proxy,group,emailMetaData) =>
            {
                email.Proxy = proxy;
                email.Group = group;
                email.MetaIds = emailMetaData;
                return email;
            },
            "ProxyId,GroupId,MetaDataId"
        );// Use the name of the column where the split should occur
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

    // Call the procedure and retrieve the output
    var parameters = new
    {
        Emails = emailTable.AsTableValuedParameter("EmailTableTypeMain"),
        GroupId = groupId,
        GroupName = groupName
    };

    // Retrieve inserted emails
    var insertedEmails = await _dbConnection.SaveDataAsync<(int EmailAccountId, string EmailAddress)>(
        "[dbo].[AddEmailsToGroupWithProxy_Create]",
        parameters
    );

    foreach (var (emailId, emailAddress) in insertedEmails)
    {
        if (emailMetadata.TryGetValue(emailAddress, out var metadata))
        {
            await _dbConnection.SaveDataAsync<dynamic>(
                "[dbo].[InsertEmailMetadata]",
                new
                {
                    EmailAccountId = emailId,
                    metadata.MailId,
                    metadata.YmreqId,
                    metadata.Wssid,
                    Cookie = metadata.Cookie
                }
            );
        }
    }
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