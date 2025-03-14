using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Subject> Subjects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-many between Section and Student with a composite key in the join table.
            modelBuilder.Entity<Section>()
                .HasMany(s => s.Students)
                .WithMany(s => s.Sections)
                .UsingEntity<Dictionary<string, object>>(
                    "SectionStudent",
                    j => j.HasOne<Student>()
                          .WithMany()
                          .HasForeignKey("StudentNumber")
                          .HasPrincipalKey(s => s.StudentNumber),
                    j => j.HasOne<Section>()
                          .WithMany()
                          .HasForeignKey("SectionId")
                          .HasPrincipalKey(s => s.Id),
                    j =>
                    {
                        j.HasKey("SectionId", "StudentNumber");
                    }
                );
        }
    }
}
