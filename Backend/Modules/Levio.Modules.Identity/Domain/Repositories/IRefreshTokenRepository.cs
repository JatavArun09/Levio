using Levio.Modules.Identity.Domain.Entities;

namespace Levio.Modules.Identity.Domain.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task<RefreshToken?> GetByTokenAsync(string tokenHash);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
    }
}
