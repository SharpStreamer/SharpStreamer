using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.Npgsql.Npgsql;

public static class Extensions
{
    public static async Task<WebApplication> MigrateDb(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        DbContext context = scope.ServiceProvider.GetRequiredService<NpgDbContext>();
        await context.Database.MigrateAsync();
        return app;
    }
}