using Microsoft.Data.SqlClient;
namespace IdentityService.Persistence;
internal static class DbExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException exception, params string[] indexes) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } sql &&
        indexes.Any(index => sql.Message.Contains(index, StringComparison.OrdinalIgnoreCase));
}
