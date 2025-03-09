using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reporting.lib.Data.Services.Proxy;
using Reporting.lib.Models.Core;
using Reporting.lib.Models.DTO;

namespace ReportingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        private readonly IProxyServices _proxyServices;

        public ProxyController(IProxyServices proxyServices)
        {
            _proxyServices = proxyServices;
        }
        
        
        [HttpGet("GetProxies")]
        public async Task<IEnumerable<Proxy>> GetEmails()
        {
            return await _proxyServices.GetAllProxies();
        }
        
        [HttpPost("UploadProxies")]
        public async Task<IActionResult> UploadProxies([FromBody] IEnumerable<ProxyDto> proxies)
        {
            try
            {
                await _proxyServices.SaveProxiesBatchAsync(proxies);
                return Ok("Proxies uploaded successfully.");
            }
            catch (Exception e)
            {
                return BadRequest($"Failed to upload proxies: {e.Message}");
            }
        }
        [HttpPost("UpdateProxiesRC")]
        public async Task<IActionResult> UpdateProxies([FromBody] IEnumerable<ProxyUpdateRegion> proxies)
        {
            try
            {
                await _proxyServices.UpdateProxiesBatchAsync(proxies);
                return Ok("Proxies updated successfully.");
            }
            catch (Exception e)
            {
                return BadRequest($"Failed to update proxies: {e.Message}");
            }
        }

        [HttpPost("ReplaceProxyProxy")]
        public async Task<IActionResult> ReplaceProxies([FromBody] IEnumerable<ProxyUpdateDto> proxies)
        {
            try
            {
                await _proxyServices.UpdateReplacedProxiesBatchAsync(proxies);
                return Ok("Proxies replaced successfully.");
            }
            catch (Exception e)
            {
                return BadRequest($"Failed to replaced proxies: {e.Message}");
            }
        }
    }
}
