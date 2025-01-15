using Cardsy.API.Options;
using Cardsy.API.Services;
using Cardsy.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
bool isDevelopment = builder.Environment.IsDevelopment();


builder.Services.Configure<Configuration>(builder.Configuration.GetSection(nameof(Configuration)));
builder.Services.AddTransient<TestService>();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Cache"));

builder.Services.AddOpenApi(options =>
{
    if (isDevelopment)
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            const string development_host = "localhost";
            document.Servers.Clear();
            var https_port = builder.Configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORTS");
            document.Servers.Add(new()
            {
                Url = $"https://{development_host}:{https_port}"
            });
            return Task.CompletedTask;
        });
    }
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

var sampleTodos = new Todo[] {
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

var settingsApi = app.MapGroup("/settings");
settingsApi.MapGet("/", (TestService service) => service.Settings);
settingsApi.MapGet("/{key}", (string key, TestService service) => service.GetSetting(key).Value is not null ? Results.Ok(service.GetSetting(key)) : Results.NotFound());

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

public record Setting(string Key, string? Value = null);

[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(Setting))]
[JsonSerializable(typeof(Setting[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
