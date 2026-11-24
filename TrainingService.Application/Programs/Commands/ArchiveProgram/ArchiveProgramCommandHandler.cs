using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Programs;
using TrainingService.Domain.Programs.Enums;

namespace TrainingService.Application.Programs.Commands.ArchiveProgram;

public sealed class ArchiveProgramCommandHandler : IRequestHandler<ArchiveProgramCommand, Result<Guid>>
{
    private readonly IGenericRepository<Program> _programRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveProgramCommandHandler(
        IGenericRepository<Program> programRepository,
        IUnitOfWork unitOfWork)
    {
        _programRepository = programRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        ArchiveProgramCommand request,
        CancellationToken cancellationToken)
    {
        var program = await _programRepository.GetByIdAsync(
            request.ProgramId,
            cancellationToken);

        if (program is null)
        {
            return Result<Guid>.NotFound($"Program with id '{request.ProgramId}' was not found.");
        }

        if (program.Status == ProgramStatus.Archived)
        {
            return Result<Guid>.Conflict("Program is already archived.");
        }

        program.Archive();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(program.Id, "Program archived successfully.");
    }
}
