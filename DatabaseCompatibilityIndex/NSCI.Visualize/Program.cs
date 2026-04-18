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

            app.Run();
        }
    }
}
