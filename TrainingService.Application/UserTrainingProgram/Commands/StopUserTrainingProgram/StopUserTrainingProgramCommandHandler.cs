using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.UserTrainingPrograms.Enums;

namespace TrainingService.Application.UserTrainingProgram.Commands.StopUserTrainingProgram;

public class StopUserTrainingProgramCommandHandler
    : IRequestHandler<StopUserTrainingProgramCommand, Result>
{
    private readonly IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public StopUserTrainingProgramCommandHandler(
        IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        StopUserTrainingProgramCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var activeProgram = _repository
            .Get(x =>
                x.UserId == userId &&
                x.Status == UserTrainingProgramStatus.Active &&
                !x.IsDeleted)
            .FirstOrDefault();

        if (activeProgram is null)
            return Result.NotFound("No active training program found.");

        activeProgram.Status = UserTrainingProgramStatus.Stopped;
        activeProgram.StoppedAt = DateTime.UtcNow;
        activeProgram.UpdatedAt = DateTime.UtcNow;

        _repository.Update(activeProgram);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}