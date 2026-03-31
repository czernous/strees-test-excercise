using Carter;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Core.Storage.InMemoryStore;
using StressTestApp.Server.Core.Storage.MarketDataStore;

var builder = WebApplication.CreateBuilder(args);

var appDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
var dbFolder = Path.Combine(appDir, "Data", "Db");
Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "stresstest.db");

builder.Services.AddDbContext<StressTestDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IStressTestDbContext>(provider =>
    provider.GetRequiredService<StressTestDbContext>());

builder.Services.AddSingleton<IFileLoader, FileLoader>();

builder.Services.AddOptionsWithValidateOnStart<CsvPaths>();
builder.Services.ConfigureOptions<CsvPathsSetup>();

builder.Services.AddSingleton<ICsvParser, CsvParser>();
builder.Services.AddSingleton<IInMemoryStore, MarketDataStore>();

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
