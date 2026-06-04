using Levio.Domain.Entities;
using Levio.Domain.Repositories;
using Levio.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Text;

namespace Levio.Infrastructure.Repositories
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

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            Context.RefreshTokens.Update(refreshToken);
        }

        
    }
}
