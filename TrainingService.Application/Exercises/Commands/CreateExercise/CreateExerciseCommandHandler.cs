using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Exercises.Dtos;
using TrainingService.Domain.Exercises;

namespace TrainingService.Application.Exercises.Commands.CreateExercise;

public class CreateExerciseCommandHandler
    : IRequestHandler<CreateExerciseCommand, Result<Guid>>
{
    private readonly IGenericRepository<Exercise> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateExerciseCommandHandler(
        IGenericRepository<Exercise> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateExerciseCommand request,
        CancellationToken cancellationToken)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Instructions = request.Instructions.Trim(),
            TargetMuscleGroup = request.TargetMuscleGroup.Trim(),
            TrackingType = request.TrackingType,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(exercise);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(exercise.Id);
    }
}