using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.UserTrainingProgram.Dtos;

namespace TrainingService.Application.UserTrainingProgram.Queries.GetUserTrainingProgramById;

public class GetUserTrainingProgramByIdQueryHandler
    : IRequestHandler<GetUserTrainingProgramByIdQuery,
        UserTrainingProgramDto?>
{
    private readonly IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> _repository;
    private readonly ICurrentUser _currentUser;

    public GetUserTrainingProgramByIdQueryHandler(
        IGenericRepository<Domain.UserTrainingPrograms.UserTrainingProgram> repository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<UserTrainingProgramDto?> Handle(
        GetUserTrainingProgramByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userTrainingProgram = await _repository.GetByIdAsync(
            request.UserTrainingProgramId,
            cancellationToken);

        if (userTrainingProgram is null ||
            userTrainingProgram.IsDeleted ||
            userTrainingProgram.UserId != _currentUser.UserId)
        {
            return null;
        }

        return new UserTrainingProgramDto(
            userTrainingProgram.Id,
            userTrainingProgram.ProgramId,
            userTrainingProgram.Status.ToString(),
            userTrainingProgram.StartedAt,
            userTrainingProgram.CompletedAt,
            userTrainingProgram.StoppedAt
        );
    }
}