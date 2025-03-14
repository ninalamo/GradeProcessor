namespace GradeProcessor.Models.Students;

public record StudentModel
{
    public int? Id { get; init; }
    public string? Fullname { get; init; }
    public string? StudentNumber { get; init; }
    public string? DateEnrolled { get; init; }
    public bool IsActive { get; init; }
}
