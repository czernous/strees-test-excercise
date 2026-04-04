using Carter;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database;
using StressTestApp.Server.Core.Exceptions;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Core.Storage.MarketDataStore;

var builder = WebApplication.CreateBuilder(args);

// Database at repository root (../Data/Db relative to project)
// Works regardless of build configuration
var projectDir = Directory.GetCurrentDirectory();
var dbFolder = Path.Combine(projectDir, "..", "Data", "Db");
var absoluteDbFolder = Path.GetFullPath(dbFolder);
Directory.CreateDirectory(absoluteDbFolder);
var dbPath = Path.Combine(absoluteDbFolder, "stresstest.db");

builder.Services.AddDbContext<StressTestDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IStressTestDbContext>(provider =>
    provider.GetRequiredService<StressTestDbContext>());

builder.Services.AddSingleton<IFileLoader, FileLoader>();

builder.Services.AddOptionsWithValidateOnStart<CsvPaths>();
builder.Services.ConfigureOptions<CsvPathsSetup>();

builder.Services.AddSingleton<ICsvParser, CsvParser>();
builder.Services.AddSingleton<IMarketDataStore, MarketDataStore>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCarter();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StressTestDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

RouteGroupBuilder api = app.MapGroup("/api");
api.MapCarter();

app.UseDefaultFiles();
app.MapStaticAssets();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

await app.RunAsync();

