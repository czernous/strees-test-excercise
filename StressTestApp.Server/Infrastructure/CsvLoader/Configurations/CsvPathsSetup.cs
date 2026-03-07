using Microsoft.Extensions.Options;

namespace StressTestApp.Server.Infrastructure.CsvLoader.Configurations
{
    public class CsvPathsSetup(IConfiguration configuration) : IConfigureOptions<CsvPaths>, IValidateOptions<CsvPaths>
    {
        private const string SectionName = "CsvPaths";
        private readonly IConfiguration _configuration = configuration;

        public void Configure(CsvPaths options)
        {
            _configuration
                .GetSection(SectionName)
                .Bind(options);
        }

        public ValidateOptionsResult Validate(string? name, CsvPaths options)
        {

            return !string.IsNullOrWhiteSpace(options.Loans) &&
                   !string.IsNullOrWhiteSpace(options.Ratings) &&
                   !string.IsNullOrWhiteSpace(options.Portfolios) ?
                   ValidateOptionsResult.Success :
                   ValidateOptionsResult.Fail("One or more invalid CSV paths");
        }
    }
}
