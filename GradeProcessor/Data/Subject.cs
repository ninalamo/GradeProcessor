namespace GradeProcessor.Data;


public class Subject : BaseEntity
{
    public Subject()
    {

    }

    public Subject(string name, string code, string? description)
    {
        Name = name;
        Code = code;
        Description = description;
    }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
}
