using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reporting.lib.Data.Services.Emails;
using Reporting.lib.Models.Core;
using Reporting.lib.Models.DTO;
using ReportingApi.Models;

namespace ReportingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailsController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailsController(IEmailService emailService)
        {
            _emailService = emailService;
        }


        [HttpGet("GetEmails")]
        public async Task<IEnumerable<EmailAccount>> GetEmails()
        {
            return await _emailService.GetAllEmailsAsync();
        }

        [HttpPost("AddEmails")]
        public async Task<IActionResult> AddEmails([FromBody] AddEmailsRequest request)
        {
            try
            {
                await _emailService.AddEmailsToGroupWithMetadataAsync(request.EmailAccounts,request.emailMetadata, request.GroupId, request.GroupName);
                return Ok();
                return Ok();
            }
            catch (Exception e)
            {
              return BadRequest(e.Message);
            }
        } 
        
        [HttpPost("UpdateEmailMetadata")]
        public async Task<IActionResult> UpdateEmailMetadata([FromBody] List<EmailMetadataDto> metadataList)
        {
            if (metadataList == null || !metadataList.Any())
            {
                return BadRequest("No metadata provided.");
            }

            try
            {
                await _emailService.UpdateEmailMetadataBatchAsync(metadataList);
                return Ok("Metadata updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating metadata: {ex.Message}");
            }
        }

        [HttpPost("AddNetworkLogs")]
        public async Task<IActionResult> AddNetworkLogs([FromBody] List<NetworkLogDto> networkLogs)
        {
            try
            {
                await _emailService.AddNetworkLogsAsync(networkLogs);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        } 
        [HttpGet("GetAllMailBoxes")]
        public async Task<IEnumerable<NetworkLogDto>> GetAllMailBoxes()
        {
            return await _emailService.GetAllMailBoxes();
            
        } 
        [HttpGet("GetAllEmailsWithMailboxes")]
        public async Task<IEnumerable<EmailMailboxDetails>> GetAllEmailsWithMailboxes()
        {
            return await _emailService.GetAllEmailsWithMailboxesDetails();
            
        }   
        [HttpDelete("DeleteAllMailboxes")]
        public async Task DeleteAllMailboxes()
        {
            await _emailService.DeleteAllMailboxes();
        }
        [HttpPost("AddMailBoxes")]
        public async Task<IActionResult> AddMailBoxes([FromBody] List<MailBoxDto> mailBoxDtos)
        {
            try
            {
                await _emailService.AddMailBoxesAsync(mailBoxDtos);
                return Ok("Mailboxes added successfully.");
            }
            catch (Exception e)
            {
                return BadRequest($"Error: {e.Message}");
            }
        }

        [HttpPost("FailEmails")]
        public async Task<IActionResult> AddEmailsToFailTable([FromBody] IEnumerable<FailedEmailDto> failedEmails)
        {
            try
            {
                await _emailService.AddFailedEmailsBatchAsync(failedEmails);
                return Ok();
            }
            catch (Exception e)
            {
              return BadRequest(e.Message);
            }
        }
        [HttpGet("GetFailEmails")]
        public async Task<IEnumerable<RetrieveFailedEmailDto>> GetEmailsFromFailedTable( int group)
        {
            return await _emailService.GetFailedEmailsByGroupAsync(group);
           
        }
        [HttpPost("DeleteBannedEmails")]
        public async Task<IActionResult> DeleteBannedEmails([FromBody] IEnumerable<string> bannedEmails)
        {
            try
            {
                await _emailService.DeleteBannedEmailsAsync(bannedEmails);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("UpdateStats")]
        public async Task<IActionResult> UpdateEmailsStats([FromBody] IEnumerable<EmailStatsUpdateDto> emails)
        {
            try
            {
                await _emailService.UpdateEmailStatsBatchAsync(emails);
                return Ok();
            }
            catch (Exception e)
            {
              return BadRequest(e.Message);
            }
        }

        [HttpPost("UpdateProxies")]
        public async Task<IActionResult> UpdateEmailProxies([FromBody] IEnumerable<EmailProxyMappingDto> emailProxyMappings)
        {
            try
            {
                await _emailService.UpdateEmailProxiesBatchAsync(emailProxyMappings);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost("DeleteEmails")]
        public async Task<IActionResult> DeleteEmailsCall([FromBody] string emails)
        {
            try
            {
                int deletedCount = await _emailService.DeleteEmailsAsync(emails);
                return Ok(new { DeletedEmails = deletedCount });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


    }
}
