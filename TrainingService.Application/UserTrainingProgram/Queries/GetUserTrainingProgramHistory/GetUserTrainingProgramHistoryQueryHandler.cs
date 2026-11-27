using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.UserTrainingProgram.Dtos;
using TrainingService.Domain.UserTrainingPrograms.Enums;

namespace TrainingService.Application.UserTrainingProgram.Queries.GetUserTrainingProgramHistory;

public class GetUserTrainingProgramHistoryQueryHandler
    : IRequestHandler<GetUserTrainingProgramHistoryQuery,
        IReadOnlyList<UserTrainingProgramDto>>
{
    private readonly IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> _repository;
    private readonly ICurrentUser _currentUser;

    public GetUserTrainingProgramHistoryQueryHandler(
        IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> repository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public Task<IReadOnlyList<UserTrainingProgramDto>> Handle(
        GetUserTrainingProgramHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var history = _repository
            .Get(x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                (x.Status == UserTrainingProgramStatus.Completed ||
                 x.Status == UserTrainingProgramStatus.Stopped))
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new UserTrainingProgramDto(
                x.Id,
                x.ProgramId,
                x.Status.ToString(),
                x.StartedAt,
                x.CompletedAt,
                x.StoppedAt
            ))
            .ToList();

        return Task.FromResult<IReadOnlyList<UserTrainingProgramDto>>(history);
    }
}