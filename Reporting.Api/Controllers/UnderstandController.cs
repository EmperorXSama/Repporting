using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Reporting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnderstandController : ControllerBase
    {
        // GET: api/Understand
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Understand/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Understand
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Understand/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/Understand/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
