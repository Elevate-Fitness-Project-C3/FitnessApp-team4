using Microsoft.EntityFrameworkCore;

namespace TrainingService.Infrastructure.Persistence;

public class TrainingDbContext : DbContext
{
    public TrainingDbContext(DbContextOptions<TrainingDbContext> options)
        : base(options)
    {
    }
}