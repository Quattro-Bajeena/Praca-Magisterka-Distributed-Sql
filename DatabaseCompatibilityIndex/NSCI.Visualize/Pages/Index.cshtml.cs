using Microsoft.AspNetCore.Mvc.RazorPages;
using NSCI.Visualize.Models;
using NSCI.Visualize.Services;

namespace NSCI.Visualize.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TestDataService _testDataService;

        public List<DatabaseInfo> Databases { get; set; } = new();
        public ComparisonData ComparisonData { get; set; } = new();

        public IndexModel(TestDataService testDataService)
        {
            _testDataService = testDataService;
        }

        public void OnGet()
        {
            Databases = _testDataService.GetAllDatabases();
            ComparisonData = _testDataService.GetComparisonData();
        }
    }
}
