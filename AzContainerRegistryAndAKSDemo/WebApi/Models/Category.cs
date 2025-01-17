namespace WebApi.Models;

public class Category : Base
{
    public Category()
    {
        Posts = new HashSet<Post>();
    }
    public int Id { get; set; } 
    public string Name { get; set; }   

    public ICollection<Post> Posts { get; set; }

}
