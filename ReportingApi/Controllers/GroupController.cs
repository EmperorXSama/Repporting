using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reporting.lib.Data.Services.Group;
using Reporting.lib.Models.Core;

namespace ReportingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpGet]
        public async Task<IEnumerable<EmailGroup>> Get()
        {
            return await _groupService.GetAllGroups();
        }
        [HttpPost("AddGroup")]
        public async Task<int> Post([FromBody] string newGroupName)
        {
            return await _groupService.AddGroup(newGroupName);
        }
    }
}
