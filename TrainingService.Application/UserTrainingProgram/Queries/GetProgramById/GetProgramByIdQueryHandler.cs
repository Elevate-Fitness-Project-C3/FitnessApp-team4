using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Programs;

namespace TrainingService.Application.UserTrainingProgram.Queries.GetProgramById;

public class GetProgramByIdQueryHandler
    : IRequestHandler<GetProgramByIdQuery, Program?>
{
    private readonly IGenericRepository<Program> _programRepository;

    public GetProgramByIdQueryHandler(
        IGenericRepository<Program> programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<Program?> Handle(
        GetProgramByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _programRepository.GetByIdAsync(
            request.ProgramId,
            cancellationToken);
    }
}