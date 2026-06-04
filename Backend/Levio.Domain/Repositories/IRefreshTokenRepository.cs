using Levio.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Domain.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task AddAsync(RefreshToken refreshToken);
        Task Update(RefreshToken refreshToken);
    }
}
