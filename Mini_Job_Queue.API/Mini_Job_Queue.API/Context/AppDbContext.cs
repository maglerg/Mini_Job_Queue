
using Mini_Job_Queue.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Mini_Job_Queue.API.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs => Set<Job>();

        public DbSet<UrlCheckResult> UrlCheckResults
            => Set<UrlCheckResult>();

        public DbSet<LogAnalysisInput> LogAnalysisInputs
            => Set<LogAnalysisInput>();

        public DbSet<LogAnalysisResult> LogAnalysisResults
            => Set<LogAnalysisResult>();
        public DbSet<AutomationRule> AutomationRules
    => Set<AutomationRule>();

        public DbSet<AutomationRuleUrl> AutomationRuleUrls
            => Set<AutomationRuleUrl>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Одна задача может содержать несколько результатов проверки URL.
            modelBuilder.Entity<Job>()
                .HasMany(job => job.Results)
                .WithOne(result => result.Job)
                .HasForeignKey(result => result.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Исходные логи: JobId — первичный ключ.
            modelBuilder.Entity<LogAnalysisInput>()
                .HasKey(input => input.JobId);

            modelBuilder.Entity<Job>()
                .HasOne(job => job.LogAnalysisInput)
                .WithOne(input => input.Job)
                .HasForeignKey<LogAnalysisInput>(input => input.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Результат анализа: JobId — первичный ключ.
            modelBuilder.Entity<LogAnalysisResult>()
                .HasKey(result => result.JobId);

            modelBuilder.Entity<Job>()
                .HasOne(job => job.LogAnalysisResult)
                .WithOne(result => result.Job)
                .HasForeignKey<LogAnalysisResult>(result => result.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LogAnalysisResult>()
                .Property(result => result.CategoryCountsJson)
                .IsRequired();

            modelBuilder.Entity<LogAnalysisResult>()
    .Property(result => result.FailedAuthenticationByHostJson)
    .IsRequired();

            modelBuilder.Entity<AutomationRule>()
    .HasMany(rule => rule.Urls)
    .WithOne(url => url.AutomationRule)
    .HasForeignKey(url => url.AutomationRuleId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AutomationRule>()
                .HasMany(rule => rule.Jobs)
                .WithOne(job => job.AutomationRule)
                .HasForeignKey(job => job.AutomationRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        }

    }
}
