using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using SharpStreamer.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("docs");

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/docs.json", "SharpStreamer.Api");
});


var provider = builder.Services.BuildServiceProvider();
// await Extensions.TestStreamer(provider.GetRequiredService<ILogger<Producer>>(), provider.GetRequiredService<ILogger<StreamSystem>>(), provider.GetRequiredService<ILogger<Consumer>>());
await SuperStreams.TestStreamer(provider.GetRequiredService<ILogger<Producer>>(), provider.GetRequiredService<ILogger<StreamSystem>>(), provider.GetRequiredService<ILogger<Consumer>>());


app.Run();