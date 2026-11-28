using MediatR;
using TrainingService.Api.Common;
using TrainingService.Api.Common.Mapping;
using TrainingService.Application.Common.Models;
using TrainingService.Application.Programs.Commands.ArchiveProgram;
using TrainingService.Application.Programs.Commands.PublishProgram;
using TrainingService.Application.Programs.Dtos;
using TrainingService.Application.Programs.Queries.GetAll;
using TrainingService.Application.Programs.Queries.GetById;

namespace TrainingService.Api.Endpoints;

public static class ProgramEndpoints
{
    public static IEndpointRouteBuilder MapProgramEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/programs")
            .WithTags("Programs");

        group.MapGet("/", async (
            ISender sender,
            CancellationToken cancellationToken,
            int pageNumber = 1,
            int pageSize = 10) =>
        {
            var result = await sender.Send(new GetAllProgramsQuery(pageNumber, pageSize), cancellationToken);
            return result.ToApiResponse();
        })
        .WithName("GetAllPrograms")
        .Produces<ApiResponse<PaginatedResult<ProgramDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<PaginatedResult<ProgramDto>>>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetProgramByIdQuery(id), cancellationToken);
            return result.ToApiResponse();
        })
        .WithName("GetProgramById")
        .Produces<ApiResponse<ProgramDetailsDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ProgramDetailsDto>>(StatusCodes.Status404NotFound);

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

        group.MapPatch("/{id:guid}/archive", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ArchiveProgramCommand(id), cancellationToken);
            return result.ToApiResponse();
        })
        .WithName("ArchiveProgram")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<Guid>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<Guid>>(StatusCodes.Status409Conflict);

        return app;
    }
}
