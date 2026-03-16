using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSCI.Visualize.Models;
using NSCI.Visualize.Services;

namespace NSCI.Visualize.Pages;

public class DatabaseModel : PageModel
{
    private readonly TestDataService _testDataService;

    public DatabaseInfo? Database { get; set; }
    public List<TestResultInfo> TestResults { get; set; } = new();
    public Dictionary<string, CategoryStats> CategoryStats { get; set; } = new();

    public DatabaseModel(TestDataService testDataService)
    {
        _testDataService = testDataService;
    }

    public IActionResult OnGet(int id)
    {
        Database = _testDataService.GetDatabase(id);
        
        if (Database == null)
        {
            return NotFound();
        }

        TestResults = _testDataService.GetTestResultsForDatabase(id);
        CategoryStats = _testDataService.GetCategoryStatsForDatabase(id);

        return Page();
    }
}
