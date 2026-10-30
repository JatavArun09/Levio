using Levio.SharedKernel.Domain;

namespace Levio.Modules.Identity.Domain.Entities
{
    public class Department : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public long? ManagerId { get; set;}
        public User? Manager { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
