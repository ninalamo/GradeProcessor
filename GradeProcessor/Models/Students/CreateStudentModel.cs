using System.ComponentModel.DataAnnotations;

namespace GradeProcessor.Models.Students;

public record CreateStudentModel
{
    [Required]
    public string? FirstName { get; init; }
    [Required]
    public string? LastName { get; init; }
    public string? MiddleName { get; init; }
    [Required]
    public string? StudentNumber { get; init; }
    [Required]
    public DateTime DateEnrolled { get; init; }
}
