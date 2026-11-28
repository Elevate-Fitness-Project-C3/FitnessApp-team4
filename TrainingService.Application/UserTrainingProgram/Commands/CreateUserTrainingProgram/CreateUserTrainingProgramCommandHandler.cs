using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.UserTrainingPrograms.Enums;

namespace TrainingService.Application.UserTrainingProgram.Commands.CreateUserTrainingProgram;

public class CreateUserTrainingProgramCommandHandler
    : IRequestHandler<CreateUserTrainingProgramCommand, Result<Guid>>
{
    private readonly IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateUserTrainingProgramCommandHandler(
        IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        CreateUserTrainingProgramCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var userTrainingProgram = new Domain.UserTrainingPrograms.UserTrainingProgram
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            ProgramId = request.ProgramId,
            Status = UserTrainingProgramStatus.Active,
            StartedAt = now,
            CreatedAt = now
        };

        await _repository.AddAsync(userTrainingProgram);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(userTrainingProgram.Id);
    }
}