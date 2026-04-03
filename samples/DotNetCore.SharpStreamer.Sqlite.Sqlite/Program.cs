using System.Reflection;
using DotNetCore.SharpStreamer;
using DotNetCore.SharpStreamer.Sqlite.Sqlite;
using DotNetCore.SharpStreamer.Storage.Sqlite;
using DotNetCore.SharpStreamer.Transport.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi("docs");

builder.Services.AddDbContext<SqliteSqliteDbContext>(options =>
{
    options.UseSqlite("Data Source=C:\\Users\\User\\Desktop\\SqliteDb\\sqlite_sqlite_sample.db");
});

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
    options.Lifetime = ServiceLifetime.Transient;
});

builder.Services
    .AddSharpStreamer("SharpStreamerSettings", Assembly.GetExecutingAssembly())
    .AddSharpStreamerStorageSqlite<SqliteSqliteDbContext>()
    .AddSharpStreamerTransportSqlite();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/docs.json", "SharpStreamer.Api");
});

app.MapControllers();

app.Run();