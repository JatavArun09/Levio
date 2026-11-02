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

        public Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return Context.RefreshTokens.Include(x =>x.User).ThenInclude(x=>x.UserRoles).ThenInclude(x=>x.Role).FirstOrDefaultAsync(x => x.TokenHash == token);
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
