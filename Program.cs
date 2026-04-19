using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Register EF Core with the SQLite provider; connection string is in appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ──────────────────────────────────────────────────────────────────
// Set up ASP.NET Core Identity with our custom ApplicationUser (which adds FullName)
// and IdentityRole for role-based access control
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password complexity rules — applied when creating or changing passwords
    options.Password.RequiredLength         = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit           = true;
    options.Password.RequireUppercase       = true;

    // Skip email confirmation for ease of use in this demo application
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()   // store Identity tables in the same SQLite DB
.AddDefaultTokenProviders();               // needed for password-reset tokens etc.

// Configure the authentication cookie behaviour
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/Account/Login";       // redirect here when not authenticated
    options.AccessDeniedPath = "/Account/AccessDenied";// redirect here when authenticated but unauthorised
    options.SlidingExpiration = true;                  // resets the session timer on each request
    options.ExpireTimeSpan   = TimeSpan.FromHours(8);  // session lasts up to 8 hours of inactivity
});

// ── Application services ──────────────────────────────────────────────────────
// Register our custom service using the interface, so controllers depend on the
// abstraction rather than the concrete type (enables mocking in unit tests)
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ── Seed database on startup ──────────────────────────────────────────────────
// Create a temporary DI scope to resolve scoped services (DbContext, UserManager, etc.)
// outside the normal request pipeline — safe to do during startup
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
// Order matters: each middleware wraps everything that follows it

if (!app.Environment.IsDevelopment())
{
    // In production, catch unhandled exceptions and show a friendly 500 page
    app.UseExceptionHandler("/Home/Error/500");
    // HSTS tells browsers to only connect via HTTPS (enforced for 30 days by default)
    app.UseHsts();
}

// Intercept non-success status codes (e.g. 404) and re-execute to the error page
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseHttpsRedirection();  // redirect HTTP → HTTPS
app.UseStaticFiles();       // serve wwwroot/ files (CSS, JS, images)
app.UseRouting();           // match incoming URLs to controller routes

// Authentication must come before Authorization
app.UseAuthentication();    // read the auth cookie and populate User
app.UseAuthorization();     // enforce [Authorize] attributes

// Default route: /Controller/Action/id — dashboard is the landing page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

// Expose the Program class as partial so the xUnit test project can reference it
// via WebApplicationFactory<Program> for integration test bootstrapping
public partial class Program { }
