using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace SharpStreamer.Api;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}