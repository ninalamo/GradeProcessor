using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GradeProcessor.Data;
using GradeProcessor.Models.Students;
using System.Globalization;
using System.Text;

namespace GradeProcessor.Controllers;

public class StudentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 10; // Change as needed

    public StudentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Students
    public async Task<IActionResult> Index(string searchTerm, int page = 1)
    {
        // Start with all students
        var query = _context.Students.AsQueryable();

        // Filter by search term if provided (searching StudentNumber, FirstName, or LastName)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(s => s.StudentNumber.Contains(searchTerm) ||
                                     s.FirstName.Contains(searchTerm) ||
                                     s.LastName.Contains(searchTerm));
        }

        // Get total record count for pagination
        int totalRecords = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);

        // Apply ordering and pagination
        var students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Map to your view model (StudentModel) as needed
        var model = students.Select(s => new StudentModel
        {
            Id = s.Id,
            StudentNumber = s.StudentNumber,
            Fullname = s.GetFullNameVariant(),
            DateEnrolled = s.DateEnrolled.ToString("dd/MM/yyyy"),
            IsActive = s.IsActive
        });

        // Pass current search and pagination info via ViewBag
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(model);
    }

    // GET: Students/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(m => m.Id == id);
        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // GET: Students/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Students/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,MiddleName,StudentNumber,DateEnrolled")] CreateStudentModel model)
    {
        if (ModelState.IsValid)
        {
            _context.Add(new Student(model.FirstName!,model.MiddleName!,model.LastName!,model.StudentNumber!,model.DateEnrolled));
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Students/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound();
        }
        return View(student);
    }

    // POST: Students/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("FirstName,LastName,MiddleName,StudentNumber,Id,IsActive")] Student student)
    {
        if (id != student.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(student);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(student.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(student);
    }

    // GET: Students/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(m => m.Id == id);
        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // POST: Students/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            _context.Students.Remove(student);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult UploadCsv()
    {
        return View();
    }

    // POST: Students/UploadCsv
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadCsv(IFormFile file)
    {
        TempData["UploadMessage"] = null;
        TempData["FailedCsv"] = null;
        TempData["FailedMessage"] = null;


        if (file == null || file.Length == 0)
        {
            TempData["UploadMessage"] = "No file uploaded.";
            return RedirectToAction(nameof(Index));
        }

        var failures = new List<string>(); // To record failures (including error details)
        int successCount = 0;

        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            // Optionally, skip header if present.
            string? header = await reader.ReadLineAsync();
            if (header == null)
            {
                TempData["UploadMessage"] = "Empty file.";
                return RedirectToAction(nameof(Index));
            }
            // If header contains "Student Number", assume it's a header line.
            if (header.Contains("Student Number"))
            {
                // Header skipped.
            }
            else
            {
                // No header; rewind.
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                reader.DiscardBufferedData();
            }

            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Expecting pipe-delimited values: e.g. 1129-22|ALFARO, christian dale|01/15/2023
                var parts = line.Split(',');
                if (parts.Length < 3)
                {
                    failures.Add($"{line} - Invalid format (expected 3 columns).");
                    continue;
                }

                var studentNumber = parts[0].Trim();
                var studentName = parts[1].Trim() + "," + parts[2].Trim();
                var dateEnrolledStr = parts[3].Trim();

                // Parse student name: split by comma.
                var nameParts = studentName.Replace("\"","").Split(',');
                if (nameParts.Length < 2)
                {
                    failures.Add($"{line} - Invalid Student Name format.");
                    continue;
                }
                var lastName = nameParts[0].Trim();
                var firstName = nameParts[1].Trim();

                // Parse Date Enrolled using MM/dd/yyyy format.
                if (!DateTime.TryParseExact(dateEnrolledStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateEnrolled))
                {
                    failures.Add($"{line}, Invalid Date Enrolled format.");
                    continue;
                }

                // Check for duplicates by Student Number or Full Name (case-insensitive)
                bool exists = await _context.Students.AnyAsync(s =>
                    s.StudentNumber == studentNumber ||
                    (EF.Functions.Collate(s.FirstName, "SQL_Latin1_General_CP1_CI_AS") == firstName &&
                     EF.Functions.Collate(s.LastName, "SQL_Latin1_General_CP1_CI_AS") == lastName));

                if (exists)
                {
                    failures.Add($"{line}, Duplicate record found.");
                    continue;
                }

                // Create new Student instance.
                var student = new Student(
                
                    firstName,
                    "",
                    lastName,
                    studentNumber,
                    dateEnrolled
                   );

                _context.Students.Add(student);
                successCount++;
            }
        }

        await _context.SaveChangesAsync();
        TempData["UploadMessage"] = $"{successCount} student(s) successfully uploaded.";

        if (failures.Any())
        {
            // Build CSV string for failed rows.
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Student Number,Student Name,Date Enrolled,Error");
            foreach (var fail in failures)
            {
                csvBuilder.AppendLine(fail);
            }
            TempData["FailedCsv"] = csvBuilder.ToString();
            TempData["FailedMessage"] = "Some records failed to upload. Click the link below to download the failed CSV.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Students/DownloadFailedCsv
    [HttpGet]
    public IActionResult DownloadFailedCsv()
    {
        if (TempData["FailedCsv"] is string csvContent)
        {
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"FailedUploads - {DateTimeOffset.Now.ToString()}.csv");
        }
        return RedirectToAction(nameof(Index));
    }

    private bool StudentExists(int id)
    {
        return _context.Students.Any(e => e.Id == id);
    }
}
