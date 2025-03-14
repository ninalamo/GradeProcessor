namespace GradeProcessor.Data;


    public class Section : BaseEntity
    {
        private readonly int _subjectId;

        public Section()
        {
            Students = new HashSet<Student>();
        }

        public Section(int subjectId, string? name) : this()
        {
            _subjectId = subjectId;
            Name = name;
        }
        public required string? Name { get; set; }

        public Subject? Subject { get; }

        public ICollection<Student> Students { get; }
    }
