using Microsoft.AspNetCore.Mvc;

namespace PollyTelemetryDemo.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CommentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet()]
        public async Task<IActionResult> Get(int commentId, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("typicode-comments");
            
            var response = await client.GetAsync(
                $"posts/{commentId}/comments", ct);

            if (response.IsSuccessStatusCode)
                return Ok(await response.Content.ReadAsStringAsync());

            return StatusCode(500);
        }
    }
}