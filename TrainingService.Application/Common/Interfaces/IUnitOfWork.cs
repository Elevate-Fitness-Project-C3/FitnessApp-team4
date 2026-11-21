namespace TrainingService.Application.Common.Interfaces;

public interface IUnitOfWork
{
 Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);

    Task AddSavePointAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task RollbackToSavePointAsync(
        string name,
        CancellationToken cancellationToken = default);
 }       