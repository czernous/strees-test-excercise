using Carter;
using StressTestApp.Server.Features.Countries.List;

namespace StressTestApp.Server.Features.Countries
{
    public class Endpoints: ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app) =>
            app.MapGet("/countries", ListCountriesHandler.Handle);
    }
}
