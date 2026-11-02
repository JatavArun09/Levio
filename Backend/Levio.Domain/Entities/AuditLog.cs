using Levio.Domain.Common;

namespace Levio.Domain.Entities
{
    public class AuditLog :BaseEntity
    {
        public long? UserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public string IpAddress { get; set; } = string.Empty;

        public string CorrelationId { get; set; } = string.Empty;
    }
}