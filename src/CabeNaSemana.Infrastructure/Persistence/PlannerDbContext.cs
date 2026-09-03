using CabeNaSemana.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CabeNaSemana.Infrastructure.Persistence;

public sealed class PlannerDbContext(DbContextOptions<PlannerDbContext> options)
    : DbContext(options)
{
    public DbSet<StudyTask> StudyTasks => Set<StudyTask>();
    public DbSet<PlannerSetting> PlannerSettings => Set<PlannerSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<StudyTask>();
        task.ToTable("StudyTasks");
        task.HasKey(item => item.Id);
        task.Property(item => item.Title).HasMaxLength(StudyTask.MaximumTitleLength).IsRequired();
        task.Property(item => item.EstimatedHours).HasPrecision(6, 2);
        task.Property(item => item.Importance).HasConversion<string>().HasMaxLength(16);
        task.Property(item => item.Status).HasConversion<string>().HasMaxLength(16);
        task.HasIndex(item => new { item.Status, item.DueDate });

        var settings = modelBuilder.Entity<PlannerSetting>();
        settings.ToTable("PlannerSettings");
        settings.HasKey(item => item.Id);
        settings.Property(item => item.WeeklyCapacityHours).HasPrecision(5, 2);
    }
}
