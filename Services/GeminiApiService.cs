using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using HtmlAgilityPack;

namespace WebMkt.Services
{
    public class GeminiApiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public GeminiApiService(HttpClient httpClient, IConfiguration config, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _config = config;
            _env = env;
        }

        public async Task<AiGenerateResponse> GenerateArticleAsync(string keyword, string tone)
        {
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) throw new Exception("Gemini API Key is missing. Bạn cần nhập API Key trong appsettings.json");

            var systemInstruction = $@"You are an expert SEO copywriter. You will write a highly engaging marketing article optimized for SEO.
Tone of voice: {tone}. Keyword: {keyword}.
Generate the output in strictly valid JSON format with the following keys:
- Title (string): A captivating SEO title (max 65 chars)
- Slug (string): A URL-friendly slug based on title
- Content (string): Full HTML article content wrapped in <h2>, <p>, <ul> tags. Do NOT wrap in markdown code blocks like ```html.
- MetaDescription (string): A compelling meta description (max 160 chars)
- MetaKeywords (string): Comma-separated keywords
- SchemaMarkup (string): A valid JSON-LD BlogPosting schema string.
The output MUST be only JSON and parsable.";

            var configuredModel = _config["Gemini:Model"] ?? "gemini-2.5-flash";
            var candidateModels = new[] { configuredModel, "gemini-2.5-flash-lite", "gemini-2.0-flash" }.Distinct().ToArray();
            string lastError = null;

            foreach (var candidateModel in candidateModels)
            {
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{candidateModel}:generateContent?key={apiKey}";
                var requestBody = new
                {
                    system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = $"Write an article about: {keyword}" } }
                        }
                    },
                    generationConfig = new { response_mime_type = "application/json" }
                };

                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    if ((int)response.StatusCode == 503 || error.Contains("\"status\": \"UNAVAILABLE\""))
                    {
                        lastError = $"Gemini model {candidateModel} unavailable: {error}";
                        continue;
                    }

                    throw new Exception($"Gemini API Error: {error}");
                }

                var responseJson = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                var contentString = responseJson?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrEmpty(contentString)) throw new Exception("Failed to get content from Gemini AI.");

                return JsonSerializer.Deserialize<AiGenerateResponse>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            throw new Exception($"Gemini API unavailable for all fallback models. Last error: {lastError}");
        }

        public async Task<string> GenerateImageAsync(string keyword, string slug)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var configuredModel = _config["Gemini:Model"] ?? "gemini-2.5-flash";
            
            var systemInstruction = "You are an expert image prompt engineer. Write a highly detailed, short English prompt to generate an image for an article. Return ONLY the prompt string. No letters or text in the image.";
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{configuredModel}:generateContent?key={apiKey}";
            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[] { new { parts = new[] { new { text = $"Generate an image prompt about: {keyword}" } } } }
            };

            var aiRes = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
            var responseJson = await aiRes.Content.ReadFromJsonAsync<GeminiResponse>();
            var promptStr = responseJson?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();
            
            if (string.IsNullOrEmpty(promptStr)) promptStr = "Modern marketing illustration for " + keyword;

            string imageUrl = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(promptStr)}?width=800&height=400&nologo=true";
            
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
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) throw new Exception("Gemini API Key is missing.");

            var systemInstruction = $@"You are an expert SEO content strategist. 
Come up with a comprehensive and SEO-optimized outline for an article.
Tone of voice: {tone}. Keyword: {keyword}.
Provide only the outline formatted with - (bullet points) and numbers, representing H2 and H3 tags. Do not output anything else.";

            var configuredModel = _config["Gemini:Model"] ?? "gemini-2.5-flash";
            var candidateModels = new[] { configuredModel, "gemini-2.5-flash-lite", "gemini-2.0-flash" }.Distinct().ToArray();
            string lastError = null;

            foreach (var candidateModel in candidateModels)
            {
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{candidateModel}:generateContent?key={apiKey}";
                var requestBody = new
                {
                    system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = $"Generate outline for: {keyword}" } }
                        }
                    }
                }; // no JSON response mime type needed here, return text

                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    if ((int)response.StatusCode == 503 || error.Contains("\"status\": \"UNAVAILABLE\""))
                    {
                        lastError = $"Gemini model {candidateModel} unavailable: {error}";
                        continue;
                    }

                    throw new Exception($"Gemini API Error: {error}");
                }

                var responseJson = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                var contentString = responseJson?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (!string.IsNullOrEmpty(contentString)) return contentString;
            }

            throw new Exception($"Gemini API unavailable for all fallback models. Last error: {lastError}");
        }

        public async Task<AiGenerateResponse> GenerateArticleFromOutlineAsync(string keyword, string tone, string outline)
        {
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) throw new Exception("Gemini API Key is missing.");

            var systemInstruction = $@"You are an expert SEO copywriter. You will write a highly engaging marketing article optimized for SEO based on the provided outline.
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

            var configuredModel = _config["Gemini:Model"] ?? "gemini-2.5-flash";
            var candidateModels = new[] { configuredModel, "gemini-2.5-flash-lite", "gemini-2.0-flash" }.Distinct().ToArray();
            string lastError = null;

            foreach (var candidateModel in candidateModels)
            {
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{candidateModel}:generateContent?key={apiKey}";
                var requestBody = new
                {
                    system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = $"Write the article for: {keyword} following the outline." } }
                        }
                    },
                    generationConfig = new { response_mime_type = "application/json" }
                };

                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    if ((int)response.StatusCode == 503 || error.Contains("\"status\": \"UNAVAILABLE\""))
                    {
                        lastError = $"Gemini model {candidateModel} unavailable: {error}";
                        continue;
                    }

                    throw new Exception($"Gemini API Error: {error}");
                }

                var responseJson = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                var contentString = responseJson?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrEmpty(contentString)) throw new Exception("Failed to get content from Gemini AI.");

                return JsonSerializer.Deserialize<AiGenerateResponse>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            throw new Exception($"Gemini API unavailable for all fallback models. Last error: {lastError}");
        }

        public async Task<AiGenerateResponse> GenerateArticleFromUrlAsync(string url, string tone, string instruction)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new Exception("URL đối thủ không được để trống.");
            if (string.IsNullOrWhiteSpace(instruction))
            {
                instruction = "Viết lại nội dung bài viết thành một bài chuẩn SEO, độc bản, dễ đọc và hiện đại. Giữ ý chính nhưng đổi cấu trúc và văn phong.";
            }

            var scrapedText = await ScrapeCompetitorUrlAsync(url);
            var excerpt = scrapedText.Length > 18000 ? scrapedText[..18000] : scrapedText;

            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) throw new Exception("Gemini API Key is missing.");

            var systemInstruction = $@"You are an expert SEO copywriter and content rewrite specialist.
You will be provided with the competitor article text below.
Rewrite it into a unique Vietnamese marketing article optimized for SEO.
Tone of voice: {tone}.
Instruction: {instruction}
Original competitor article text:
{excerpt}

Generate the output in strictly valid JSON format with the following keys:
- Title (string): A captivating SEO title (max 65 chars)
- Slug (string): A URL-friendly slug based on title
- Content (string): Full HTML article content wrapped in <h2>, <p>, <ul> tags. Do NOT wrap in markdown code blocks like ```html.
- MetaDescription (string): A compelling meta description (max 160 chars)
- MetaKeywords (string): Comma-separated keywords
- SchemaMarkup (string): A valid JSON-LD BlogPosting schema string.
The output MUST be only JSON and parsable.";

            var configuredModel = _config["Gemini:Model"] ?? "gemini-2.5-flash";
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{configuredModel}:generateContent?key={apiKey}";
            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = $"Rewrite the competitor article into a new SEO-friendly Vietnamese article." } }
                    }
                },
                generationConfig = new { response_mime_type = "application/json" }
            };

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {error}");
            }

            var responseJson = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var contentString = responseJson?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(contentString)) throw new Exception("Failed to get content from Gemini AI.");

            return JsonSerializer.Deserialize<AiGenerateResponse>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<string> ScrapeCompetitorUrlAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new Exception("URL đối thủ không hợp lệ.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124 Safari/537.36");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var articleNodes = doc.DocumentNode.SelectNodes("//article|//main|//div[contains(@class,'article')]|//div[contains(@class,'post')]|//div[contains(@class,'entry')]|//div[contains(@class,'content')]|//div[contains(@id,'content')]");
            var extracted = new List<string>();

            if (articleNodes != null)
            {
                foreach (var node in articleNodes)
                {
                    var text = ExtractNodeText(node);
                    if (!string.IsNullOrWhiteSpace(text)) extracted.Add(text);
                }
            }

            if (!extracted.Any())
            {
                var fallbackNodes = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//p");
                if (fallbackNodes != null)
                {
                    foreach (var node in fallbackNodes)
                    {
                        var text = ExtractNodeText(node);
                        if (!string.IsNullOrWhiteSpace(text)) extracted.Add(text);
                    }
                }
            }

            if (!extracted.Any()) throw new Exception("Không thể trích xuất nội dung bài viết từ URL đối thủ.");

            var finalText = string.Join("\n\n", extracted);
            return WebUtility.HtmlDecode(finalText.Trim());
        }

        private string ExtractNodeText(HtmlNode node)
        {
            return string.Join(" ", node.DescendantsAndSelf()
                .Where(n => n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText))
                .Select(n => n.InnerText.Trim()))
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("  ", " ");
        }

        public async Task<string> RewriteContentAsync(string content, string instruction)
        {
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) throw new Exception("Gemini API Key is missing.");

            var systemInstruction = $@"You are an expert AI editor and copywriter.
You will be provided with some text/html and an instruction on how to modify it.
Apply the instruction to the text perfectly. Return ONLY the modified text/html. Do NOT wrap it in markdown formatting strings like ```html.";

            var configuredModel = _config["Gemini:Model"] ?? "gemini-2.5-flash";
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{configuredModel}:generateContent?key={apiKey}";
            
            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = $"Instruction: {instruction}\n\nText to edit:\n{content}" } }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {error}");
            }

            var responseJson = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var contentString = responseJson?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(contentString)) throw new Exception("Failed to get content from Gemini AI.");

            return contentString.Trim();
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate> Candidates { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part> Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }
    }
}
