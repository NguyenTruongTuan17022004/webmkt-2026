using System.Text.Json;

namespace WebMkt.Services
{
    public interface IAiService
    {
        Task<AiGenerateResponse> GenerateArticleAsync(string keyword, string tone);
        Task<string> GenerateOutlineAsync(string keyword, string tone);
        Task<AiGenerateResponse> GenerateArticleFromOutlineAsync(string keyword, string tone, string outline);
        Task<AiGenerateResponse> GenerateArticleFromUrlAsync(string url, string tone, string instruction);
        Task<string> GenerateImageAsync(string keyword, string slug);
        Task<string> RewriteContentAsync(string content, string instruction);
    }

    public class AiGenerateResponse
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string Slug { get; set; }
        public object SchemaMarkup { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
