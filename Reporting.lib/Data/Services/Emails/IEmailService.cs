using Reporting.lib.Models.DTO;

namespace Reporting.lib.Data.Services.Emails;

public interface IEmailService
{
    Task<IEnumerable<EmailAccount>> GetAllEmailsAsync();
    Task AddEmailsToGroupAsync(IEnumerable<CreateEmailAccountDto> emails, int? groupId = null, string? groupName = null);
    Task<IEnumerable<EmailAccount>> GetEmailsByGroupAsync(int groupId);
}