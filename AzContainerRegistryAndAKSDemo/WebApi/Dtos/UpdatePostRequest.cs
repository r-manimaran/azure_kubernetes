namespace WebApi.Dtos;

public class UpdatePostRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int CategoryId { get; set; }
}
