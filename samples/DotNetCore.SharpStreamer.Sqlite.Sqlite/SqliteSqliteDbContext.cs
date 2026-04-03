using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.SharpStreamer.Sqlite.Sqlite;

public class SqliteSqliteDbContext(DbContextOptions<SqliteSqliteDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }   
}