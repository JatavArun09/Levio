using Levio.Domain.Common;
using Levio.Domain.Enums;

namespace Levio.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; }
        public ICollection<UserRoleType> UserRoles { get; set; } = new List<UserRoleType>();
    }
}
