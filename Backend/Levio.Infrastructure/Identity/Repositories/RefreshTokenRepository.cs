using Levio.Modules.Identity.Domain.Entities;
using Levio.Modules.Identity.Domain.Repositories;
using Levio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Levio.Infrastructure.Identity.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext Context;
        public RefreshTokenRepository(ApplicationDbContext context)
        {
            Context = context;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return Context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            await Context.RefreshTokens.AddAsync(refreshToken);
        }

        public Task UpdateAsync(RefreshToken refreshToken)
        {
            Context.RefreshTokens.Update(refreshToken);
            return Task.CompletedTask;
        }

        
    }
}
