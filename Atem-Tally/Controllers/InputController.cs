using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TallyServer.Hubs;
using TallyServer.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Atem_Tally.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InputController : ControllerBase
    {
        private AtemService AtemService { get; set; }
        private IHubContext<TallyHub, ITallyClient> TallyHub { get; set; }

        InputController(
            AtemService atemService,
            IHubContext<TallyHub, ITallyClient> tallyHub)
        {
            AtemService = atemService; ;
            TallyHub = tallyHub;
        }


        // GET: api/<InputController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<InputController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<InputController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<InputController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<InputController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
