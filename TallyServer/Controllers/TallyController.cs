using Microsoft.AspNetCore.Mvc;
using TallyServer.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TallyServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TallyController : ControllerBase
    {
        TallyController(
            AtemService atemService)
        {

        }


        // GET: api/<TallyController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<TallyController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<TallyController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<TallyController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<TallyController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
