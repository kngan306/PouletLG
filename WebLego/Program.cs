using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using WebLego.DataSet.GdrService;
using WebLego.Services;
using Microsoft.AspNetCore.Authentication;
using WebLego.Models;

var builder = WebApplication.CreateBuilder(args);

// Thêm logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Đăng ký các dịch vụ
builder.Services.AddScoped<MembershipService>();
builder.Services.AddHostedService<MembershipUpdateService>();
builder.Services.AddHostedService<OrderExpirationService>();
builder.Services.AddHostedService<ContestWinnerUpdateService>();
builder.Services.AddHostedService<ContestReminderService>();
builder.Services.AddHostedService<ResetRankService>();
builder.Services.AddSession();
builder.Services.Configure<VNPayConfig>(builder.Configuration.GetSection("VNPay"));
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<EmailService>();
builder.Services.AddMemoryCache(); // Thêm IMemoryCache
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.ClaimActions.MapJsonKey("picture", "picture", "url");
    options.SaveTokens = true;
});

builder.Services.AddDbContext<DbpouletLgv5Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBPouletLGConnection")));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.LoginPath = "/Auth/Login";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();