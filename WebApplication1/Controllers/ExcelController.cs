using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using WebApplication1.Models;

public class ExcelController : Controller
{
    public IActionResult Index()
    {
        return View(new List<AssignmentData>());
    }

    [HttpPost]
    public IActionResult Upload(IFormFile file, string sheetName)
    {
        var dataList = new List<AssignmentData>();

        if (file != null && file.Length > 0)
        {
            // Ensure the uploads folder exists
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string fileName = Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, fileName);

            // Save the file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Trim whitespace from the sheet name; case-insensitive matching will be applied when reading.
            sheetName = sheetName?.Trim();

            dataList = ReadExcelFile(filePath, sheetName);
        }

        return View("Index", dataList);
    }

    private List<AssignmentData> ReadExcelFile(string filePath, string sheetName)
    {
        var dataList = new List<AssignmentData>();

        var index = 2;

        if(sheetName == "MASTERLIST")
        {
            index = 13;
        }else if(sheetName == "Assignment Data")
        {
            index = 2;
        }



            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = WorkbookFactory.Create(stream);
                var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();

                // Locate the sheet using a case-insensitive match on the sheet name
                ISheet sheet = null;
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var currentSheet = workbook.GetSheetAt(i);
                    if (string.Equals(currentSheet.SheetName, sheetName, StringComparison.OrdinalIgnoreCase))
                    {
                        sheet = currentSheet;
                        break;
                    }
                }

                if (sheet != null)
                {
                    // Loop through rows starting from row 2 (adjust if your header is in a different row)
                    for (var rowIndex = index; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        IRow currentRow = sheet.GetRow(rowIndex);
                        if (currentRow == null)
                            continue;

                        // Adjust the column index as needed (15 here means the 16th column)
                        ICell cell = currentRow.GetCell(15);
                        if (cell == null)
                            continue;

                        string cellValue = string.Empty;

                        // Check if the cell contains a formula
                        if (cell.CellType == CellType.Formula)
                        {
                            try
                            {
                                var evaluatedCell = evaluator.Evaluate(cell);
                                switch (evaluatedCell.CellType)
                                {
                                    case CellType.String:
                                        cellValue = evaluatedCell.StringValue;
                                        break;
                                    case CellType.Numeric:
                                        cellValue = evaluatedCell.NumberValue.ToString();
                                        break;
                                    case CellType.Boolean:
                                        cellValue = evaluatedCell.BooleanValue.ToString();
                                        break;
                                    default:
                                        cellValue = cell.ToString();
                                        break;
                                }
                            }
                            catch (NPOI.SS.Formula.FormulaParseException)
                            {
                                // Fallback: use the cached formula result if evaluation fails
                                if (cell.CachedFormulaResultType == CellType.String)
                                {
                                    cellValue = cell.StringCellValue;
                                }
                                else if (cell.CachedFormulaResultType == CellType.Numeric)
                                {
                                    cellValue = cell.NumericCellValue.ToString();
                                }
                                else if (cell.CachedFormulaResultType == CellType.Boolean)
                                {
                                    cellValue = cell.BooleanCellValue.ToString();
                                }
                                else
                                {
                                    cellValue = cell.ToString();
                                }
                            }
                        }
                        else
                        {
                            // For non-formula cells, convert to string directly.
                            cellValue = cell.ToString();
                        }

                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            dataList.Add(new AssignmentData { ColumnData = cellValue });
                        }
                    }
                }
            }

        return dataList;
    }
}
