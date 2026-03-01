using System.Linq.Expressions;

namespace MicroCredit.Domain.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);

    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
