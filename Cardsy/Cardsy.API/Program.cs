using Cardsy.API.Endpoints.Games.Concentration;
using Cardsy.API.Options;
using Cardsy.API.Serialization;
using Cardsy.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Dapper;
using Cardsy.Data.Database;
using Microsoft.Extensions.Configuration;

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
        builder.Logging
            .ClearProviders()
            .SetMinimumLevel(LogLevel.Debug)
            .AddConsole()
            .AddDebug();

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

        builder.Services.AddKeyedSingleton<IDbConnectionFactory>(
            DatabaseNames.Cardsy, 
            (sp, o) => new NpgsqlDbConnectionFactory(builder.Configuration.GetConnectionString(nameof(DatabaseNames.Cardsy))));

        builder.Services.AddScoped<IConcentrationService, ConcentrationService>();

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Cache");
        });

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

        builder.Services.AddProblemDetails();

        var app = builder.Build();

        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseHttpsRedirection();
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            StatusCodeSelector = ex => ex switch
            {
                BadHttpRequestException => StatusCodes.Status400BadRequest,
                NotImplementedException => StatusCodes.Status501NotImplemented,
                _ => StatusCodes.Status500InternalServerError
            }
        });

        var gamesAPI = app.MapGroup("/games");
        gamesAPI.MapConcentrationEndpoints();

        app.Run();
    }
}
