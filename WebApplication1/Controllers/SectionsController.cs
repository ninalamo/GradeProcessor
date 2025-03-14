using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using WebApplication1.Data;

public class SectionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SectionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var sections = await _context.Sections
            .Include(s => s.Students)
            .ToListAsync();
        return View(sections);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Section section)
    {
        if (ModelState.IsValid)
        {
            section.Id = Guid.NewGuid().ToString();
            section.IsActive = true;
            _context.Sections.Add(section);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(section);
    }

    public async Task<IActionResult> Manage(string id)
    {
        var section = await _context.Sections
            .Include(s => s.Students)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (section == null) return NotFound();
        return View(section);
    }

    [HttpGet]
    public async Task<IActionResult> GetStudents()
    {
        var students = await _context.Students
            .Select(s => new { s.StudentNumber, s.FirstName, s.LastName })
            .ToListAsync();
        return Json(students);
    }

    [HttpPost]
    public async Task<IActionResult> AddStudents(string sectionId, List<string> selectedStudentNumbers)
    {
        var section = await _context.Sections
            .Include(s => s.Students)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return NotFound();

        var studentsToAdd = await _context.Students
            .Where(s => selectedStudentNumbers.Contains(s.StudentNumber))
            .ToListAsync();

        foreach (var student in studentsToAdd)
        {
            if (!section.Students.Contains(student))
            {
                // Check conflict: ensure student is not in any other section with the same subject.
                bool conflict = await _context.Sections
                    .Where(s => s.SubjectId == section.SubjectId && s.Id != section.Id)
                    .AnyAsync(s => s.Students.Any(st => st.StudentNumber == student.StudentNumber));
                if (!conflict)
                {
                    section.Students.Add(student);
                }
                // Optionally, handle conflict (e.g., notify the user).
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Manage), new { id = sectionId });
    }

    [HttpPost]
    public async Task<IActionResult> AddStudent(string sectionId, string studentNumber, string firstName, string lastName)
    {
        var section = await _context.Sections
            .Include(s => s.Students)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return NotFound();

        var student = await _context.Students
            .Include(s => s.Sections)
            .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

        if (student == null)
        {
            student = new Student
            {
                StudentNumber = studentNumber,
                FirstName = firstName,
                LastName = lastName,
                DateEnrolled = DateTime.UtcNow
            };
            _context.Students.Add(student);
        }

        // If the student is not already in the current section,
        // ensure they're not enrolled in any other section with the same subject.
        if (!section.Students.Any(st => st.StudentNumber == student.StudentNumber))
        {
            bool conflict = await _context.Sections
                .Where(s => s.Id != section.Id)
                .AnyAsync(s => s.Students.Any(st => st.StudentNumber == student.StudentNumber));
            if (conflict)
            {
                ModelState.AddModelError("", "This student is already enrolled in a section for the same subject.");
                return RedirectToAction(nameof(Manage), new { id = sectionId });
            }
        }

        section.Students.Add(student);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Manage), new { id = sectionId });
    }

    [HttpPost]
    public async Task<IActionResult> UploadStudents(string sectionId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var section = await _context.Sections
            .Include(s => s.Students)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null)
            return NotFound();

        using var stream = new StreamReader(file.OpenReadStream());
        var jsonContent = await stream.ReadToEndAsync();

        List<StudentImportModel> students;
        try
        {
            students = JsonConvert.DeserializeObject<List<StudentImportModel>>(jsonContent);
        }
        catch
        {
            return BadRequest("Invalid JSON format.");
        }

        foreach (var item in students)
        {
            // Split studentName by comma, where first token is Last Name and second is First Name.
            var nameParts = item.StudentName.Split(',', StringSplitOptions.RemoveEmptyEntries);
            string lastName = nameParts.Length > 0 ? nameParts[0].Trim() : "";
            string firstName = nameParts.Length > 1 ? nameParts[1].Trim() : "";

            // Parse dateEnrolled from "dd/MM/yyyy" format.
            DateTime dateEnrolled;
            try
            {
                dateEnrolled = DateTime.ParseExact(item.DateEnrolled, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch
            {
                // Skip this record if the date is invalid.
                continue;
            }

            var existingStudent = await _context.Students.FindAsync(item.StudentNumber);
            if (existingStudent == null)
            {
                existingStudent = new Student
                {
                    StudentNumber = item.StudentNumber,
                    LastName = lastName,
                    FirstName = firstName,
                    DateEnrolled = dateEnrolled
                };
                _context.Students.Add(existingStudent);
            }

            if (!section.Students.Contains(existingStudent))
            {
                bool conflict = await _context.Sections
                    .Where(s => s.Id == section.SubjectId && s.Id != section.Id)
                    .AnyAsync(s => s.Students.Any(st => st.StudentNumber == existingStudent.StudentNumber));
                if (!conflict)
                {
                    section.Students.Add(existingStudent);
                }
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Manage), new { id = sectionId });
    }

    // Helper model matching your JSON file structure.
    private class StudentImportModel
    {
        public string StudentNumber { get; set; }
        public string StudentName { get; set; } // Format: "LASTNAME, FIRSTNAME"
        public string DateEnrolled { get; set; }  // Format: "dd/MM/yyyy"
    }
}
