using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SharpStreamer.Api;
using SharpStreamer.EntityFrameworkCore.Npgsql;
using SharpStreamer.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("docs");

builder.Services.AddDbContext<ApiDbContext>(options =>
{
    options.UseNpgsql(
        "Pooling=True;Maximum Pool Size=100;Minimum Pool Size=1;Connection Idle Lifetime=60;Host=localhost;Port=5432;Database=amm_exchange_db;Username=root;Password=root");
});

builder.Services
    .AddSharpStreamerRabbitMq(Assembly.GetExecutingAssembly())
    .AddSharpPersistenceNpgSql<ApiDbContext>();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/docs.json", "SharpStreamer.Api");
});

app.Run();