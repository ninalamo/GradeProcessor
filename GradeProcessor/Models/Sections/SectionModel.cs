using GradeProcessor.Data;

namespace GradeProcessor.Models.Sections
{
    public record SectionModel
    {
        public string? Name { get; init; }

        public int? SubjectId { get; init; }
        public string? SubjectTitle { get; init; }
        public int? Id { get; init; }
        public int StudentCount { get; init; }

    }
}
