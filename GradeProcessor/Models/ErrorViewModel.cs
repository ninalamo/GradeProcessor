namespace GradeProcessor.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public record PaginationModel
{
    public string ActionName { get; set; }
    public int? Id { get; set; }  // Optional – if needed for routes (e.g., Manage)
    public string SearchTerm { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

