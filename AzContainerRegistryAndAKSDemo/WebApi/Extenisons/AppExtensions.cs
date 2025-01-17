using WebApi.Data;

namespace WebApi.Extenisons;

public static class AppExtensions
{
    public static void AddDatabaseService(this IServiceCollection services)
    {

    }

    public static async void ApplyMigration(this IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.CreateScope())
        {
            await using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
        }
    }
}