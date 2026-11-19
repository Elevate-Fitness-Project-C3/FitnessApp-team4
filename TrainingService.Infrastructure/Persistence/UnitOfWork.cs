using TrainingService.Application.Common.Interfaces;

namespace TrainingService.Infrastructure.Persistence;

public class UnitOfWork(TrainingDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }
}