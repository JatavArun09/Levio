using Levio.SharedKernel.Domain;

namespace Levio.Modules.Identity.Domain.Entities
{
    public class UserRole : BaseEntity
    {
        public long UserId { get; set; }
        public User User { get; set; } = null!;
        public long RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}
