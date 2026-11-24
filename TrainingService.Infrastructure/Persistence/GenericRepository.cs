using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Common;

namespace TrainingService.Infrastructure.Persistence;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly TrainingDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(TrainingDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }
    
    public IQueryable<T> Get(Expression<Func<T, bool>> predicate)
    {
        return DbSet.Where(predicate);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        DbSet.Remove(entity);
    }
}
