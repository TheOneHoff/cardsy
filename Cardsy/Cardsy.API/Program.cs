using System.Text.Json.Serialization;
using Cardsy.API.Endpoints.Games.Concentration;
using Cardsy.API.Infrastructure.Handlers;
using Cardsy.API.Options;
using Cardsy.API.Serialization;
using Cardsy.Data;
using Cardsy.Data.Games.Concentration;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

internal class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        var builder = WebApplication.CreateBuilder(args);
        bool isDevelopment = builder.Environment.IsDevelopment();


        builder.Services.Configure<Configuration>(builder.Configuration.GetSection(nameof(Configuration)));

        builder.Host.UseSerilog();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            //options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<BoardSize>());
        });

        builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
            //options.UseModel(ApplicationDbContextModel.Instance);
        });

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

        if (isDevelopment)
        {
            builder.Services.AddExceptionHandler<DevelopmentExceptionHandler>();
        }
        else
        {
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        }
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseHttpsRedirection();
        app.UseExceptionHandler();

        var gamesAPI = app.MapGroup("/games");
        gamesAPI.MapConcentrationEndpoints();

        app.Run();
    }
}
