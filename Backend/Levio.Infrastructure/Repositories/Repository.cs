using Levio.Domain.Common;
using Levio.Domain.Repositories;
using Levio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
namespace Levio.Infrastructure.Repositories
{
    public class Repository<T> :IRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext Context;
        protected readonly DbSet<T> DbSet;

        public Repository(ApplicationDbContext context)
        {
            Context = context;
            DbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(long id)
        {
            return await DbSet.FindAsync(id);
        }
        public async Task<List<T>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await DbSet.Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity) 
        {        
            await DbSet.AddAsync(entity);
        }
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await DbSet.AddRangeAsync(entities);
        }

        public void Update(T entity)
        {
            DbSet.Update(entity);
        }

        public void Delete(T entity) { 
            DbSet.Remove(entity);
        }

        public IQueryable<T> Query()
        {
            return DbSet.AsQueryable();
        }
    }
}
