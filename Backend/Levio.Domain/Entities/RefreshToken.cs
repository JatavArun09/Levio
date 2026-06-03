using Levio.Domain.Common;

namespace Levio.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public long UserId { get; set; }

        public User User { get; set; } = null!;

        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public bool IsRevoked { get; set; }
    }
}