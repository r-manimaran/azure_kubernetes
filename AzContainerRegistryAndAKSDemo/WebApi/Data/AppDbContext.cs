using Microsoft.EntityFrameworkCore;
using WebApi.Data.Configurations;
using WebApi.Models;

namespace WebApi.Data;

public class AppDbContext:DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PostConfiguration());
        
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
    }
}
