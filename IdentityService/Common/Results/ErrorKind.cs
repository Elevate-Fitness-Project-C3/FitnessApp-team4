namespace IdentityService.Common.Results;

public enum ErrorKind
{
    Failure,
    Unexpected,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    Gone,
    TooManyRequests,
    AlreadyExists,
}