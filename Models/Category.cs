using System.ComponentModel.DataAnnotations;

namespace WebMkt.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(150)]
        public string Slug { get; set; }

        public string Description { get; set; }

        // Mối quan hệ: Một Category có nhiều bài viết
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
