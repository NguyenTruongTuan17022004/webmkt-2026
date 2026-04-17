using Microsoft.AspNetCore.Mvc;
using WebMkt.Services;

namespace WebMkt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        public class GenerateArticleRequest
        {
            public string Keyword { get; set; }
            public string Tone { get; set; }
        }

        public class GenerateFromUrlRequest
        {
            public string Url { get; set; }
            public string Tone { get; set; }
            public string Instruction { get; set; }
        }

        [HttpPost("write")]
        public async Task<IActionResult> Write([FromBody] GenerateArticleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Keyword))
            {
                return BadRequest(new { success = false, message = "Keyword is required." });
            }

            try
            {
                var tone = string.IsNullOrWhiteSpace(request.Tone) ? "Professional" : request.Tone;
                var result = await _aiService.GenerateArticleAsync(request.Keyword, tone);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("write-from-url")]
        public async Task<IActionResult> WriteFromUrl([FromBody] GenerateFromUrlRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest(new { success = false, message = "URL is required." });
            }

            try
            {
                var tone = string.IsNullOrWhiteSpace(request.Tone) ? "Professional" : request.Tone;
                var instruction = string.IsNullOrWhiteSpace(request.Instruction)
                    ? "Rewrite the competitor article into a unique, modern SEO-friendly article in Vietnamese."
                    : request.Instruction;

                var result = await _aiService.GenerateArticleFromUrlAsync(request.Url, tone, instruction);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
