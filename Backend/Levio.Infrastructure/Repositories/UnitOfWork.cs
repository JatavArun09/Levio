using Levio.Domain.Repositories;
using Levio.Infrastructure.Data;

namespace Levio.Infrastructure.Repositories
{
    public class UnitOfWork: IUnitOfWork
    {
        private readonly ApplicationDbContext Context;

        public UnitOfWork(ApplicationDbContext context)
        {
            Context = context;
        }

        public async Task<int>SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
    }
}
