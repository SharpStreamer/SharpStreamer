using System.Reflection;
using DotNetCore.SharpStreamer;
using DotNetCore.SharpStreamer.Npgsql.Npgsql;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using DotNetCore.SharpStreamer.Transport.Npgsql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi("docs");

builder.Services.AddDbContext<NpgDbContext>(options =>
{
    options.UseNpgsql("Pooling=True;Maximum Pool Size=100;Minimum Pool Size=1;Connection Idle Lifetime=60;Host=localhost;Port=5432;Database=npg_npg_sample;Username=admin;Password=admin");
});
builder.Services
    .AddSharpStreamer("SharpStreamerSettings", Assembly.GetExecutingAssembly())
    .AddSharpStreamerStorageNpgsql<NpgDbContext>()
    .AddSharpStreamerTransportNpgsql();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/docs.json", "SharpStreamer.Api");
});

app.MapControllers();

app.Run();