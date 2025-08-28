using System.Reflection;
using SharpStreamer.Api;
using SharpStreamer.EntityFrameworkCore.Npgsql;
using SharpStreamer.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("docs");

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