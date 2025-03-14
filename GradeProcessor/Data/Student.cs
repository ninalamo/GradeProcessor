namespace GradeProcessor.Data;


public class Student : BaseEntity
{
    public Student()
    {
        Sections = new HashSet<Section>();
        IsActive = true;
    }

    public Student(string firstName, string middleName, string lastName, string studentNumber, DateTime dateEnrolled) : this()
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        StudentNumber = studentNumber;
        DateEnrolled = dateEnrolled;
    }

    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? MiddleName { get; private set; }
    public string? StudentNumber { get; private set; }
    public DateTime DateEnrolled { get; private set; }

    public ICollection<Section> Sections { get; }

    public void FixStudentNumber(string studentNumber) => StudentNumber = studentNumber;

    public void Rename(string firstName, string middleName, string lastName)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
    }

    public void Enroll(Section section) => Sections.Add(section);

    public string? GetFullName() => $"{FirstName} {MiddleName} {LastName}".ToUpper();
    public string? GetFullNameVariant() => $"{LastName}, {FirstName} {MiddleName}".ToUpper();


}
