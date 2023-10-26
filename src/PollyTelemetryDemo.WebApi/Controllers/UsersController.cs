using Microsoft.AspNetCore.Mvc;

namespace PollyTelemetryDemo.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public UsersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet()]
        public async Task<IActionResult> Get(int userId)
        {
            var client = _httpClientFactory.CreateClient("typicode-users");
            
            var response = await client.GetAsync(
                $"users/{userId}");

            if (response.IsSuccessStatusCode)
                return Ok(await response.Content.ReadAsStringAsync());

            return StatusCode(500);
        }
    }
}
