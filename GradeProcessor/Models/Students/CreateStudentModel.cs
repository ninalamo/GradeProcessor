namespace GradeProcessor.Models.Students;

public record CreateStudentModel
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? MiddleName { get; init; }
    public string? StudentNumber { get; init; }
}
