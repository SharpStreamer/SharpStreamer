using System.Reflection;
using DotNetCore.SharpStreamer;
using DotNetCore.SharpStreamer.RabbitMq.Npgsql;
using DotNetCore.SharpStreamer.Storage.Npgsql;
using DotNetCore.SharpStreamer.Transport.RabbitMq;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi("docs");

builder.Services.AddDbContext<RabbitNpgDbContext>(options =>
{
    options.UseNpgsql("Pooling=True;Maximum Pool Size=100;Minimum Pool Size=1;Connection Idle Lifetime=60;Host=localhost;Port=5435;Database=rabbit_npg_sample;Username=postgres;Password=postgres");
});

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
    options.Lifetime = ServiceLifetime.Transient;
});

builder.Services
    .AddSharpStreamer("SharpStreamerSettings", Assembly.GetExecutingAssembly())
    .AddSharpStreamerStorageNpgsql<RabbitNpgDbContext>()
    .AddSharpStreamerTransportRabbitMq();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/docs.json", "SharpStreamer.Api");
});

app.MapControllers();

await app.MigrateDb();

app.Run();