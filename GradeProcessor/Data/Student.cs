namespace GradeProcessor.Data;


public class Student : BaseEntity
{
    public Student()
    {
        Sections = new HashSet<Section>();
    }

    public Student(string firstName, string middleName, string lastName, string studentNumber) : this()
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        StudentNumber = studentNumber;
    }

    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? MiddleName { get; private set; }
    public required string? StudentNumber { get; set; }

    public ICollection<Section> Sections { get; }

    public void FixStudentNumber(string studentNumber) => StudentNumber = studentNumber;

    public void Rename(string firstName, string middleName, string lastName)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
    }

    public void Enroll(Section section) => Sections.Add(section);
    

}
