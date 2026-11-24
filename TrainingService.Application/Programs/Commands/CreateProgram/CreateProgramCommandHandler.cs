using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Programs;

namespace TrainingService.Application.Programs.Commands.CreateProgram;

public class CreateProgramCommandHandler(IGenericRepository<Program> programRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProgramCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateProgramCommand request,
        CancellationToken cancellationToken)
    {
        var program = new Program( request.Goal,
            request.Level,
            request.DurationWeeks,
            request.DaysPerWeek,
            request.AccessTier,
            request.Description);
        
        await programRepository.AddAsync(program);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(
            program.Id, "Program created successfully.");
    }
}