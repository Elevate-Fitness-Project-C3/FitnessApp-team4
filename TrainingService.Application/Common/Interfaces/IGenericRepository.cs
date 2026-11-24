using System.Linq.Expressions;
using TrainingService.Domain.Common;

namespace TrainingService.Application.Common.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    IQueryable<T> Get(Expression<Func<T, bool>> predicate);
    Task<T?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<T>> GetAllAsync();

    Task AddAsync(T entity);

    void Update(T entity);

    void Delete(T entity);
}