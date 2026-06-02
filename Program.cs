using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Services;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
var databaseConnectionString = builder.Configuration.GetConnectionString("SupabasePostgres");

if (!HasUsableValue(databaseConnectionString))
{
    throw new InvalidOperationException("Connection string 'ConnectionStrings:SupabasePostgres' is required.");
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddOptions<AdminAccountOptions>()
    .Bind(builder.Configuration.GetSection(AdminAccountOptions.SectionName))
    .Validate(options => HasUsableValue(options.Username), "AdminAccount:Username is required.")
    .Validate(options => HasUsableValue(options.Password), "AdminAccount:Password is required.")
    .Validate(options => isDevelopment || options.Password.Length >= 12, "AdminAccount:Password must be at least 12 characters outside Development.")
    .ValidateOnStart();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(databaseConnectionString);
});
builder.Services.AddOptions<CloudinaryOptions>()
    .Bind(builder.Configuration.GetSection(CloudinaryOptions.SectionName))
    .Validate(options => HasUsableValue(options.CloudName), "Cloudinary:CloudName is required.")
    .Validate(options => HasUsableValue(options.ApiKey), "Cloudinary:ApiKey is required.")
    .Validate(options => HasUsableValue(options.ApiSecret), "Cloudinary:ApiSecret is required.")
    .Validate(options => HasUsableValue(options.Folder), "Cloudinary:Folder is required.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IFileStorageService, CloudinaryFileStorageService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "StudentPortal.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = isDevelopment
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls.Count > 0
        ? string.Join(", ", app.Urls)
        : "http://localhost:5025";

    app.Logger.LogInformation("Student Portal is running at: {Addresses}", addresses);
    Console.WriteLine();
    Console.WriteLine($"Student Portal is running at: {addresses}");
    Console.WriteLine();
});

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var schemaPath = Path.Combine(app.Environment.ContentRootPath, "Data", "SupabaseSchema.sql");

    if (File.Exists(schemaPath))
    {
        var schemaSql = File.ReadAllText(schemaPath);
        dbContext.Database.ExecuteSqlRaw(schemaSql);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

static bool HasUsableValue(string? value)
{
    return !string.IsNullOrWhiteSpace(value)
        && !value.Contains("YOUR-", StringComparison.OrdinalIgnoreCase);
}
