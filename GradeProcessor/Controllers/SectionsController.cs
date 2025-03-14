using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradeProcessor.Data;
using GradeProcessor.Models.Sections;
using System.Globalization;
using System.Text;
using GradeProcessor.Models.Students;

namespace GradeProcessor.Controllers;

public class SectionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SectionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Sections/
    public async Task<IActionResult> Index(string searchTerm, int page = 1)
    {
        const int PageSize = 10;

        // Build the query: include related Subject and Students.
        var query = _context.Sections
            .Include(s => s.Subject)
            .Include(s => s.Students)
            .AsQueryable();

        // Filter by search term if provided.
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(s =>
                s.Name.Contains(searchTerm) ||
                (s.Subject != null && s.Subject.Name.Contains(searchTerm)));
        }

        // Count total records for pagination.
        int totalRecords = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);

        // Retrieve the current page of data.
        var sections = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Pass pagination info via ViewBag.
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(sections);
    }



    // GET: /Sections/Create
    public IActionResult Create()
    {
        // Assuming you want to select a subject from existing subjects.
        ViewBag.Subjects = _context.Subjects.ToList();
        return View();
    }

    // POST: /Sections/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSectionModel section, int subjectId)
    {
        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null || subject.Id == default)
        {
            ModelState.AddModelError(nameof(section.SubjectId), "Invalid subject.");
        }

        if (ModelState.IsValid)
        {
            // Set the subject on the section. (Make sure Section.Subject is now read/write.)

            _context.Sections.Add(new Section(subject!.Id, section.Name));
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Subjects = _context.Subjects.ToList();
        return View(section);
    }


    // POST: /Sections/AddStudents
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStudents(int sectionId, List<int> studentIds)
    {
        var section = await _context.Sections
            .Include(s => s.Students)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return NotFound();

        var studentsToAdd = await _context.Students
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync();

        foreach (var student in studentsToAdd)
        {
            // Check: a student can only be enrolled in a section if not already in another section with the same subject.
            bool alreadyEnrolledInSameSubject = await _context.Sections
                .Where(s => s.Subject.Id == section.Subject.Id && s.Id != section.Id)
                .AnyAsync(s => s.Students.Any(st => st.Id == student.Id));

            if (!alreadyEnrolledInSameSubject)
            {
                // Avoid duplicates within the same section.
                if (!section.Students.Any(st => st.Id == student.Id))
                {
                    section.Students.Add(student);
                }
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Manage), new { id = sectionId });
    }

    // POST: /Sections/RemoveStudent
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudent(int sectionId, int studentId)
    {
        var section = await _context.Sections
            .Include(s => s.Students)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return NotFound();

        var student = section.Students.FirstOrDefault(s => s.Id == studentId);
        if (student != null)
        {
            section.Students.Remove(student);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Manage), new { id = sectionId });
    }

    public async Task<IActionResult> Manage(int id, int page = 1)
    {
        const int PageSize = 10;

        // Load the section with its Subject and all enrolled Students.
        var section = await _context.Sections
            .Include(s => s.Subject)
            .Include(s => s.Students)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (section == null)
            return NotFound();

        // Paginate the enrolled students.
        // NOTE: If section.Students is not an IQueryable, we can convert it to one.
        var allEnrolled = section.Students.AsQueryable().OrderBy(s => s.StudentNumber);
        int totalEnrolled = allEnrolled.Count();
        int totalPages = (int)Math.Ceiling(totalEnrolled / (double)PageSize);
        var pagedStudents = allEnrolled.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        // Pass the paginated list via ViewBag.
        ViewBag.PagedStudents = pagedStudents;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        // For the enrollment dropdown, fetch students not yet enrolled in this section.
        ViewBag.AllStudents = await _context.Students
            .Where(x => !x.Sections.Any(s => s.Id == id))
            .ToListAsync();

        return View(section);
    }


    // POST: Sections/UploadStudentsCsv
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadStudentsCsv(int sectionId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["UploadMessage"] = "No file uploaded.";
            return RedirectToAction(nameof(Manage), new { id = sectionId });
        }

        var section = await _context.Sections
            .Include(s => s.Students)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return NotFound();

        var failures = new List<string>();
        int successCount = 0;

        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            // Optionally, skip header if present.
            string? header = await reader.ReadLineAsync();
            if (header != null && header.Contains("Student Number"))
            {
                // header line is skipped.
            }
            else
            {
                // If no header, rewind.
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                reader.DiscardBufferedData();
            }

            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Expected format: Student Number | Student Name (e.g., ALFARO, christian dale) | Date Enrolled (MM/dd/yyyy)
                var parts = line.Split('|');
                if (parts.Length < 3)
                {
                    failures.Add($"{line} - Invalid format (expected 3 columns).");
                    continue;
                }

                string studentNumber = parts[0].Trim();
                string studentName = parts[1].Trim();
                string dateEnrolledStr = parts[2].Trim();

                // Parse studentName: expect "LASTNAME, firstname"
                var nameParts = studentName.Split(',');
                if (nameParts.Length < 2)
                {
                    failures.Add($"{line} - Invalid student name format.");
                    continue;
                }
                string lastName = nameParts[0].Trim();
                string firstName = nameParts[1].Trim();

                // Parse date enrolled using "MM/dd/yyyy" format.
                if (!DateTime.TryParseExact(dateEnrolledStr, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateEnrolled))
                {
                    failures.Add($"{line} - Invalid Date Enrolled format.");
                    continue;
                }

                // Check if student exists by student number.
                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);
                if (student == null)
                {
                    // Create new student.
                    student = new Student(firstName,string.Empty,lastName, studentNumber, dateEnrolled);
                   
                    _context.Students.Add(student);
                }

                // Check if the student is already enrolled in a section for the same subject.
                bool conflict = await _context.Sections
                    .Where(s => s.Subject.Id == section.Subject.Id)
                    .AnyAsync(s => s.Students.Any(st => st.StudentNumber == studentNumber));

                if (conflict)
                {
                    failures.Add($"{line} - Student already enrolled in a section for the same subject.");
                    continue;
                }

                // Check if student already added to this section.
                if (section.Students.Any(st => st.StudentNumber == studentNumber))
                {
                    failures.Add($"{line} - Student already enrolled in this section.");
                    continue;
                }

                // Add student to section.
                section.Students.Add(student);
                successCount++;
            }
        }

        await _context.SaveChangesAsync();
        TempData["UploadMessage"] = $"{successCount} student(s) uploaded successfully.";
        if (failures.Any())
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Student Number|Student Name|Date Enrolled|Error");
            foreach (var fail in failures)
            {
                csvBuilder.AppendLine(fail);
            }
            TempData["FailedCsv"] = csvBuilder.ToString();
            TempData["FailedMessage"] = "Some records failed to upload. Click the link below to download the failed CSV.";
        }

        return RedirectToAction(nameof(Manage), new { id = sectionId });
    }

    // GET: Sections/DownloadFailedCsv
    [HttpGet]
    public IActionResult DownloadFailedCsv()
    {
        if (TempData["FailedCsv"] is string csvContent)
        {
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", "FailedUploads.csv");
        }
        return RedirectToAction(nameof(Index));
    }
}
