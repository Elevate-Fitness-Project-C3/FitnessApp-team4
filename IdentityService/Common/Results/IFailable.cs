namespace IdentityService.Common.Results;
public interface IFailable<TSelf> { static abstract TSelf FromErrors(IEnumerable<Error> errors); }
