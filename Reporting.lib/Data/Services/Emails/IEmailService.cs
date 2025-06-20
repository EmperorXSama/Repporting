using Reporting.lib.Models.DTO;

namespace Reporting.lib.Data.Services.Emails;

public interface IEmailService
{
    Task<IEnumerable<EmailAccount>> GetAllEmailsAsync();

    Task AddEmailsToGroupWithMetadataAsync(
        IEnumerable<CreateEmailAccountDto> emails,
        Dictionary<string, EmailMetadataDto> emailMetadata,
        int? groupId = null,
        string? groupName = null);
    Task<IEnumerable<EmailAccount>> GetEmailsByGroupAsync(int groupId);
    Task UpdateEmailStatsBatchAsync(IEnumerable<EmailStatsUpdateDto> emailAccounts);
    Task UpdateEmailProxiesBatchAsync(IEnumerable<EmailProxyMappingDto> emailProxyMappings);
    Task<int> DeleteEmailsAsync(string emailText);
    Task AddFailedEmailsBatchAsync(IEnumerable<FailedEmailDto> failedEmails);
    Task<IEnumerable<RetrieveFailedEmailDto>> GetFailedEmailsByGroupAsync(int groupId);
    Task UpdateEmailMetadataBatchAsync(IEnumerable<EmailMetadata> metadataList);
    Task DeleteBannedEmailsAsync(IEnumerable<string> bannedEmails);
    Task AddNetworkLogsAsync(IEnumerable<NetworkLogDto> networkLogs);
    Task AddMailBoxesAsync(IEnumerable<MailBoxDto> mailBoxDtos);
    Task<IEnumerable<NetworkLogDto>> GetAllMailBoxes();
    Task<IEnumerable<EmailMailboxDetails>> GetAllEmailsWithMailboxesDetails();
    Task UpdateEmailMetadataBatchAsync(IEnumerable<EmailMetadataDto> metadataList);
    Task SetMailboxPackActiveAsync(List<string> emailAddresses, int packNumber);
    Task DeActivateMailboxesOnDelete(List<string> emailAddresses);
    Task DeleteAllMailboxes(List<string> emailAddresses);
}