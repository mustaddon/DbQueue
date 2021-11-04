using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Example.RestService.MsSql
{
    [ApiController]
    [Route("[controller]")]
    public class ValuesController : ControllerBase
    {

        [HttpGet]
        public async Task<int> Get() => await Task.FromResult(123);

    }
}
