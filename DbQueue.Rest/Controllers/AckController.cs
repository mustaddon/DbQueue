using Microsoft.AspNetCore.Mvc;

namespace DbQueue.Rest.Controllers
{
    [ApiController]
    [Route("dbq/[controller]")]
    public class AckController : ControllerBase
    {
        [HttpPost("{key}")]
        public virtual Task Commit(string key)
            => Service.Ack(HttpContext, key, true);

        [HttpDelete("{key}")]
        public virtual Task Unlock(string key)
            => Service.Ack(HttpContext, key, false);
    }
}
