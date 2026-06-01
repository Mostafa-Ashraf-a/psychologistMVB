using Microsoft.EntityFrameworkCore;
using MentalHealthAssessment.Domain.Entities;

namespace MentalHealthAssessment.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<PatientTestSession> PatientTestSessions { get; set; }
        public DbSet<PatientResponse> PatientResponses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Test entity
            modelBuilder.Entity<Test>(entity =>
            {
                entity.ToTable("Tests");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.DeliveryMode).HasConversion<int>();
            });

            // Configure Question entity
            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("Questions");
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Text).IsRequired().HasMaxLength(1000);
                entity.HasOne<Test>()
                      .WithMany(t => t.Questions)
                      .HasForeignKey(q => q.TestId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Option entity
            modelBuilder.Entity<Option>(entity =>
            {
                entity.ToTable("Options");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Text).IsRequired().HasMaxLength(500);
                entity.Property(o => o.Tag).HasMaxLength(100);
                entity.HasOne<Question>()
                      .WithMany(q => q.Options)
                      .HasForeignKey(o => o.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PatientTestSession entity
            modelBuilder.Entity<PatientTestSession>(entity =>
            {
                entity.ToTable("PatientTestSessions");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.PatientId).IsRequired().HasMaxLength(100);
                entity.Property(s => s.CurrentDeliveryMode).HasConversion<int>();
                entity.HasOne(s => s.Test)
                      .WithMany()
                      .HasForeignKey(s => s.TestId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure PatientResponse entity
            modelBuilder.Entity<PatientResponse>(entity =>
            {
                entity.ToTable("PatientResponses");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.MappedTag).HasMaxLength(100);
                entity.HasOne(r => r.PatientTestSession)
                      .WithMany(s => s.Responses)
                      .HasForeignKey(r => r.PatientTestSessionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Question)
                      .WithMany()
                      .HasForeignKey(r => r.QuestionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Option)
                      .WithMany()
                      .HasForeignKey(r => r.OptionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
