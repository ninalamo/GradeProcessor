using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GradeProcessor.Data;

public partial class ApplicationDbContext : IdentityDbContext
{
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Student> Students { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Subject>().HasKey(x => x.Id);
        builder.Entity<Subject>().Property(x => x.Name).IsRequired();

        builder.Entity<Section>().HasKey(x => x.Id);
        builder.Entity<Section>().Property(x => x.Name).IsRequired();
        builder.Entity<Section>()
            .Property<int>("_subjectId")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("SubjectId")
            .IsRequired();

        builder.Entity<Section>().HasOne<Subject>(s => s.Subject).WithMany().HasForeignKey("_subjectId");


        builder.Entity<Student>().HasKey(x => x.Id);
        builder.Entity<Student>().Property(x => x.FirstName).IsRequired();
        builder.Entity<Student>().Property(x => x.LastName).IsRequired();
        builder.Entity<Student>().Property(x => x.StudentNumber).IsRequired();
        builder.Entity<Student>().HasIndex(x => x.StudentNumber).IsUnique();


    }


}
public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}