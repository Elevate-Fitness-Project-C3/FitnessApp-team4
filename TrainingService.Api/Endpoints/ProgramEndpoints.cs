using MediatR;
using TrainingService.Api.Common;
using TrainingService.Api.Common.Mapping;
using TrainingService.Application.Programs.Commands.PublishProgram;

namespace TrainingService.Api.Endpoints;

public static class ProgramEndpoints
{
    public static IEndpointRouteBuilder MapProgramEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/programs")
            .WithTags("Programs");

        group.MapPatch("/{id:guid}/publish", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new PublishProgramCommand(id), cancellationToken);
            return result.ToApiResponse();
        })
        .WithName("PublishProgram")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<Guid>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<Guid>>(StatusCodes.Status409Conflict);

        return app;
    }
}
