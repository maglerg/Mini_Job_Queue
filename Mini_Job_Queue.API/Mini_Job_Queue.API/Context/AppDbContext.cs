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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Job>()
                .HasMany(job => job.Results)
                .WithOne(result => result.Job)
                .HasForeignKey(result => result.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
