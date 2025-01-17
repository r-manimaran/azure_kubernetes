namespace WebApi.Models;

public class Post: Base
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int? CategoryId { get; set; }
    public Category Category { get; set; }
}
