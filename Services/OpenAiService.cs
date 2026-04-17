using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebMkt.Services
{
    public class OpenAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public OpenAiService(HttpClient httpClient, IConfiguration config, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _config = config;
            _env = env;
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        }

        public async Task<AiGenerateResponse> GenerateArticleAsync(string keyword, string tone)
        {
            var apiKey = _config["OpenAi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) throw new Exception("OpenAI API Key is missing.");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var systemPrompt = $@"You are an expert SEO copywriter. You will write a highly engaging marketing article optimized for SEO.
Tone of voice: {tone}. Keyword: {keyword}.
Generate the output in strictly valid JSON format with the following keys:
- Title (string): A captivating SEO title (max 65 chars)
- Slug (string): A URL-friendly slug based on title
- Content (string): Full HTML article content wrapped in <h2>, <p>, <ul> tags. Do NOT wrap in markdown code blocks like ```html.
- MetaDescription (string): A compelling meta description (max 160 chars)
- MetaKeywords (string): Comma-separated keywords
- SchemaMarkup (string): A valid JSON-LD BlogPosting schema string.
The output MUST be only JSON and parsable.";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Write an article about: {keyword}" }
                },
                response_format = new { type = "json_object" },
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            var contentString = responseJson?.Choices?.FirstOrDefault()?.Message?.Content;

            if (contentString == null) throw new Exception("Failed to get content from AI.");

            var generatedData = JsonSerializer.Deserialize<AiGenerateResponse>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return generatedData;
        }

        public async Task<string> GenerateImageAsync(string keyword, string slug)
        {
            string imageUrl = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString("Modern illustration for " + keyword)}?width=800&height=400&nologo=true";
            var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "posts");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            string fileName = slug + "-" + Guid.NewGuid().ToString().Substring(0, 4) + ".jpg";
            string filePath = Path.Combine(uploadsFolder, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
            return $"/uploads/posts/{fileName}";
        }

        public async Task<string> GenerateOutlineAsync(string keyword, string tone)
        {
            var apiKey = _config["OpenAi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) throw new Exception("OpenAI API Key is missing.");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var systemPrompt = $@"You are an expert SEO content strategist. 
Come up with a comprehensive and SEO-optimized outline for an article.
Tone of voice: {tone}. Keyword: {keyword}.
Provide only the outline formatted with - (bullet points) and numbers, representing H2 and H3 tags. Do not output anything else.";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Generate outline for: {keyword}" }
                },
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            var contentString = responseJson?.Choices?.FirstOrDefault()?.Message?.Content;

            if (contentString == null) throw new Exception("Failed to get outline from AI.");

            return contentString;
        }

        public async Task<AiGenerateResponse> GenerateArticleFromOutlineAsync(string keyword, string tone, string outline)
        {
            var apiKey = _config["OpenAi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) throw new Exception("OpenAI API Key is missing.");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var systemPrompt = $@"You are an expert SEO copywriter. You will write a highly engaging marketing article optimized for SEO based on the provided outline.
Tone of voice: {tone}. Keyword: {keyword}.
Strictly follow this Outline:
{outline}

Generate the output in strictly valid JSON format with the following keys:
- Title (string): A captivating SEO title (max 65 chars)
- Slug (string): A URL-friendly slug based on title
- Content (string): Full HTML article content wrapped in <h2>, <p>, <ul> tags based exactly on the outline. Do NOT wrap in markdown code blocks like ```html.
- MetaDescription (string): A compelling meta description (max 160 chars)
- MetaKeywords (string): Comma-separated keywords
- SchemaMarkup (string): A valid JSON-LD BlogPosting schema string.
The output MUST be only JSON and parsable.";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Write the article for: {keyword} following the outline" }
                },
                response_format = new { type = "json_object" },
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            var contentString = responseJson?.Choices?.FirstOrDefault()?.Message?.Content;

            if (contentString == null) throw new Exception("Failed to get content from AI.");

            var generatedData = JsonSerializer.Deserialize<AiGenerateResponse>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return generatedData;
        }

        public Task<AiGenerateResponse> GenerateArticleFromUrlAsync(string url, string tone, string instruction)
        {
            throw new NotImplementedException();
        }

        public async Task<string> RewriteContentAsync(string content, string instruction)
        {
            throw new NotImplementedException();
        }

        private class OpenAiResponse
        {
            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("message")]
            public Message Message { get; set; }
        }

        private class Message
        {
            [JsonPropertyName("content")]
            public string Content { get; set; }
        }
    }
}
