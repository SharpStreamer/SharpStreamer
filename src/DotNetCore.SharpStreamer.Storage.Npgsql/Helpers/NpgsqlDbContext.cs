using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Helpers;

public class NpgsqlDbContext(DbContextOptions<NpgsqlDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureSharpStreamerNpgsql();
        base.OnModelCreating(modelBuilder);
    }
}