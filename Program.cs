using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SupabasePostgres");
    options.UseNpgsql(connectionString);
});
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddHttpClient<IImageStorageService, CloudinaryImageStorageService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
