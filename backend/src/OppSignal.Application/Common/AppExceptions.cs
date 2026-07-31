namespace OppSignal.Application.Common;

/// <summary>Base for application errors mapped to HTTP problem responses by the API middleware.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
    public abstract int StatusCode { get; }
    public virtual string Title => "Request could not be completed";
}

/// <summary>404 — entity not found or not owned by the caller.</summary>
public sealed class NotFoundException(string message = "Not found") : AppException(message)
{
    public override int StatusCode => 404;
    public override string Title => "Not found";
}

/// <summary>409 — conflicting state (e.g. duplicate).</summary>
public sealed class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => 409;
    public override string Title => "Conflict";
}

/// <summary>402 — action blocked by the user's plan limits; carries the upgrade hint.</summary>
public sealed class PlanLimitException(string message) : AppException(message)
{
    public override int StatusCode => 402;
    public override string Title => "Plan limit reached";
}

/// <summary>400 — semantic validation failure not caught by FluentValidation.</summary>
public sealed class BadRequestException(string message) : AppException(message)
{
    public override int StatusCode => 400;
    public override string Title => "Invalid request";
}

/// <summary>401 — authentication failed / token invalid.</summary>
public sealed class UnauthorizedAppException(string message = "Unauthorized") : AppException(message)
{
    public override int StatusCode => 401;
    public override string Title => "Unauthorized";
}

/// <summary>403 — authenticated but not permitted (e.g. unverified email).</summary>
public sealed class ForbiddenAppException(string message) : AppException(message)
{
    public override int StatusCode => 403;
    public override string Title => "Forbidden";
}
