Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Register data repositories
builder.Services.AddScoped<ThrdCtrl2.Data.UserRepository>();
builder.Services.AddScoped<ThrdCtrl2.Data.InventoryRepository>();
builder.Services.AddScoped<ThrdCtrl2.Data.AuditRepository>();
builder.Services.AddScoped<ThrdCtrl2.Data.EmailService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ThrdCtrl2.Data.ReCaptchaService>();

// Add Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Test/Login";
        options.AccessDeniedPath = "/Test/AccessDenied";
    });

var app = builder.Build();

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var userRepo = scope.ServiceProvider.GetRequiredService<ThrdCtrl2.Data.UserRepository>();
    userRepo.MigrateDatabase();
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
    pattern: "{controller=Test}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
