using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Data;

public class Student
{
    [Key]
    public string? StudentNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateTime? DateEnrolled { get; set; }

    // Navigation property to allow many-to-many relationship
    public ICollection<Section> Sections { get; set; } = new HashSet<Section>();
}

public class Section
{
    public string? Id { get; set; }
    public required string Description { get; set; }
    public bool IsActive { get; set; }
    public string? Id

    public ICollection<Student>? Students { get; }

    public Section()
    {
        Students = new HashSet<Student>();
    }

    public Section(string? id) : this()
    {
        Id = id;
    }
}



public class Subject
{
    [Key]
    public string? Id { get; set; }  // e.g., "ELECTIVE3"
    public required string Name { get; set; }  // e.g., "Elective 3"
    public bool IsActive { get; set; }
}

