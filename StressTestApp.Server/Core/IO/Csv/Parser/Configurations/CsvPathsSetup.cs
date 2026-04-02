using Microsoft.Extensions.Options;

namespace StressTestApp.Server.Core.IO.Csv.Parser.Configurations
{
    public class CsvPathsSetup(
        IConfiguration configuration,
        IHostEnvironment environment) : IConfigureOptions<CsvPaths>, IValidateOptions<CsvPaths>
    {
        private const string SectionName = "CsvPaths";
        private readonly IConfiguration _configuration = configuration;
        private readonly IHostEnvironment _environment = environment;

        public void Configure(CsvPaths options)
        {
            _configuration
                .GetSection(SectionName)
                .Bind(options);

            options.Loans = ResolvePath(options.Loans);
            options.Portfolios = ResolvePath(options.Portfolios);
            options.Ratings = ResolvePath(options.Ratings);
        }

        public ValidateOptionsResult Validate(string? name, CsvPaths options)
        {
            if (string.IsNullOrWhiteSpace(options.Loans) ||
                string.IsNullOrWhiteSpace(options.Ratings) ||
                string.IsNullOrWhiteSpace(options.Portfolios))
            {
                return ValidateOptionsResult.Fail("One or more CSV paths are missing.");
            }

            var missingFiles = new List<string>();

            if (!File.Exists(options.Loans))
            {
                missingFiles.Add($"Loans: {options.Loans}");
            }

            if (!File.Exists(options.Portfolios))
            {
                missingFiles.Add($"Portfolios: {options.Portfolios}");
            }

            if (!File.Exists(options.Ratings))
            {
                missingFiles.Add($"Ratings: {options.Ratings}");
            }

            return missingFiles.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail($"One or more CSV files were not found. {string.Join("; ", missingFiles)}");
        }

        private string ResolvePath(string path) =>
            Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, path));
    }
}
