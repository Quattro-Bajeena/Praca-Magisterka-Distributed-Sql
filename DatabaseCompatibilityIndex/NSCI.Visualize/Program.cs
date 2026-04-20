using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSCI.Data;
using NSCI.Visualize.Services;

namespace NSCI.Visualize
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();

            string connectionString = builder.Configuration.GetConnectionString("StatDb")
                ?? "Host=localhost;Port=5432;Username=postgres;Password=password;Database=NewSqlCompatibilityIndex";

            builder.Services.AddDbContextFactory<NsciDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddSingleton<TestDataService>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            // API: update failure category for a single test result
            app.MapPost("/api/failure-category", (
                [FromBody] UpdateFailureCategoryRequest req,
                TestDataService testDataService) =>
            {
                bool found = testDataService.UpdateFailureCategory(req.TestResultId, req.Category);
                return found ? Results.Ok() : Results.NotFound();
            });

            app.Run();
        }
    }
}

// Request model for the failure category API endpoint
public record UpdateFailureCategoryRequest(int TestResultId, FailureCategory? Category);
