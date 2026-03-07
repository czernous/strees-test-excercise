using Carter;

namespace StressTestApp.Server.Features.Calculations;

public class Endpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/calculations", Create.CreateCalculationHandler.Handle)
            .WithName("CreateCalculation")
            .WithDescription(@"""Creates a new calculation based on the provided house price changes for different countries.
            Returns summary information about the created calculation, including its unique identifier, creation timestamp, duration, and input parameters.""")
            .WithTags("Calculations");

        app.MapGet("/calculations", List.ListCalculationsHandler.Handle)
            .WithName("ListCalculations")
            .WithDescription("List summary information about all calculations, including their inputs but not results.")
            .WithTags("Calculations");

        app.MapGet("/calculations/{id:guid}", GetById.GetCalculationHandler.Handle)
            .WithName("GetCalculation")
            .WithDescription("Get detailed information about a specific calculation, including its inputs and results.")
            .WithTags("Calculations");
    }
}
