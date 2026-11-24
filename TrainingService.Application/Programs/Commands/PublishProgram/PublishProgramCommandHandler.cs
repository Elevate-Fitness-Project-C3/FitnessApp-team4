using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Programs;
using TrainingService.Domain.Programs.Enums;

namespace TrainingService.Application.Programs.Commands.PublishProgram;

public sealed class PublishProgramCommandHandler : IRequestHandler<PublishProgramCommand, Result<Guid>>
{
    private readonly IGenericRepository<Program> _programRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishProgramCommandHandler(
        IGenericRepository<Program> programRepository,
        IUnitOfWork unitOfWork)
    {
        _programRepository = programRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        PublishProgramCommand request,
        CancellationToken cancellationToken)
    {
        var program = await _programRepository.GetByIdAsync(
            request.ProgramId);

        if (program is null)
        {
            return Result<Guid>.NotFound($"Program with id '{request.ProgramId}' was not found.");
        }

        if (program.Status == ProgramStatus.Published)
        {
            return Result<Guid>.Conflict("Program is already published.");
        }

        program.Publish();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(program.Id, "Program published successfully.");
    }
}
