using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.UserTrainingProgram.Dtos;
using TrainingService.Domain.UserTrainingPrograms.Enums;

namespace TrainingService.Application.UserTrainingProgram.Queries.GetActiveUserTrainingProgram;

public class GetActiveUserTrainingProgramQueryHandler
    : IRequestHandler<GetActiveUserTrainingProgramQuery, UserTrainingProgramDto?>
{
    private readonly IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> _repository;
    private readonly ICurrentUser _currentUser;

    public GetActiveUserTrainingProgramQueryHandler(
        IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> repository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public Task<UserTrainingProgramDto?> Handle(
        GetActiveUserTrainingProgramQuery request,
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
            return Task.FromResult<UserTrainingProgramDto?>(null);
        
        var response = new UserTrainingProgramDto(
            activeProgram.Id,
            activeProgram.ProgramId,
            activeProgram.Status.ToString(),
            activeProgram.StartedAt,
            activeProgram.CompletedAt,
            activeProgram.StoppedAt
        );

        return Task.FromResult<UserTrainingProgramDto?>(response);
    }
}