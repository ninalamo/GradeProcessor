using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradeProcessor.Data;
using GradeProcessor.Models.Sections;

namespace GradeProcessor.Controllers;

public class SectionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SectionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Sections/
    public async Task<IActionResult> Index()
    {
        var sections = await _context.Sections
            .Include(s => s.Students)
            .Include(s => s.Subject)
            .Select(s => new
            {
                s.Id,
                s.Name,
                SubjectName = s.Subject != null ? s.Subject.Name : "No Subject",
                StudentCount = s.Students.Count
            })
            .ToListAsync();

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
        if(subject == null || subject.Id == default)
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

    // GET: /Sections/Manage/5
    public async Task<IActionResult> Manage(int id)
    {
        var section = await _context.Sections
            .Include(s => s.Students)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (section == null) return NotFound();

        // For enrolling existing students, send all available students to the view.
        ViewBag.AllStudents = await _context.Students.ToListAsync();
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
}
