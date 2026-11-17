namespace IdentityService.Common.Results;

public enum ErrorKind
{
    Failure = 0,
    Validation = 2,
    Conflict = 3,
    Unauthorized = 5,
    Forbidden = 6,
    AlreadyExists = 9,
}
