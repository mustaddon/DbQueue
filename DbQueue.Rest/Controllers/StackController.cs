using Microsoft.AspNetCore.Mvc;

namespace DbQueue.Rest.Controllers
{
    [ApiController]
    [Route("dbq/[controller]")]
    public class StackController : ControllerBase
    {
        [HttpPost("{name}")]
        public virtual Task Push(string name, [FromQuery] string? type, [FromQuery] DateTime? availableAfter, [FromQuery] DateTime? removeAfter, [FromQuery] string? separator)
            => Service.Push(HttpContext, name, type, availableAfter, removeAfter, separator);

        [HttpGet("{name}")]
        public virtual Task Pop(string name, [FromQuery] bool? useAck, [FromQuery] int? ackDeadline)
            => Service.Pop(HttpContext, true, name, useAck, ackDeadline);

        [HttpGet("{name}/[action]")]
        public virtual Task Peek(string name, [FromQuery] int? index)
            => Service.Peek(HttpContext, true, name, index);

        [HttpGet("{name}/[action]")]
        public virtual Task<long> Count(string name)
            => Service.Count(HttpContext, name);

        [HttpDelete("{name}")]
        public virtual Task Clear(string name, [FromQuery] string? type, [FromQuery] string? separator)
            => Service.Clear(HttpContext, name, type, separator);
    }
}
