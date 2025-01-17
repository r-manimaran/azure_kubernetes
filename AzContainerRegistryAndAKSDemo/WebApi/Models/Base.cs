namespace WebApi.Models;

public abstract class Base
{
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime LastModifiedOn { get; set; } = DateTime.UtcNow;
}
