using MicroCredit.Domain.Interfaces;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MicroCredit.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly MicroCreditDbContext _context;

    public GenericRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(T entity)
        => await _context.Set<T>().AddAsync(entity);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _context.Set<T>().ToListAsync();

    public async Task<T> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<T>().FindAsync(id);
        if (entity == null)
            throw new InvalidOperationException($"Entity of type {typeof(T).Name} with id '{id}' was not found.");
        return entity;
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
        => await _context.Set<T>().Where(expression).ToListAsync();

    public void Update(T entity)
        => _context.Set<T>().Update(entity);

    public void Remove(T entity)
        => _context.Set<T>().Remove(entity);
}
