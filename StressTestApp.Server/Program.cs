using Carter;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Data;
using StressTestApp.Server.Infrastructure.CsvLoader;
using StressTestApp.Server.Infrastructure.CsvLoader.Configurations;
using StressTestApp.Server.Infrastructure.CsvLoader.Interfaces;
using StressTestApp.Server.Persistence.MarketDataStore;
using StressTestApp.Server.Persistence.MarketDataStore.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var dbFolder = Path.Combine("Data", "Db");
Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "stresstest.db");

builder.Services.AddDbContext<StressTestDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddOptionsWithValidateOnStart<CsvPaths>();
builder.Services.ConfigureOptions<CsvPathsSetup>();

builder.Services.AddSingleton<ICsvDataLoader, CsvLoader>();
builder.Services.AddSingleton<IMarketDataStore, MarketDataStore>();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

await app.RunAsync();
