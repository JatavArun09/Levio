namespace Levio.SharedKernel.Domain;

public sealed class RequestConflictException(string message) : DomainException(message);
public sealed class ResourceForbiddenException(string message) : DomainException(message);
public sealed class BusinessRuleException(string message) : DomainException(message);
