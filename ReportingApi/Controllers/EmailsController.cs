using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reporting.lib.Data.Services.Emails;
using Reporting.lib.Models.Core;
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
                await _emailService.AddEmailsToGroupAsync(request.EmailAccounts, request.GroupId, request.GroupName);
                return Ok();
            }
            catch (Exception e)
            {
              return BadRequest(e.Message);
            }
        }
    }
}
