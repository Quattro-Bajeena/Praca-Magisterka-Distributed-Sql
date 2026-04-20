using Microsoft.EntityFrameworkCore;
using NSCI.Data;
using NSCI.Visualize;
using NSCI.Visualize.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

string connectionString = builder.Configuration.GetConnectionString("StatDb")
    ?? "Host=localhost;Port=5432;Username=postgres;Password=password;Database=NewSqlCompatibilityIndex";

builder.Services.AddDbContextFactory<NsciDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<TestDataService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
