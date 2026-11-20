using BilisimPersoneli.Domain.Enums;

namespace BilisimPersoneli.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}
