using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;

string? defaultEnvironment = null;
#if DEBUG
// Visual Studio can launch the Debug executable without applying a launch profile.
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    && !args.Any(arg => arg.TrimStart('-', '/').Split('=', 2)[0]
        .Equals(HostDefaults.EnvironmentKey, StringComparison.OrdinalIgnoreCase)))
{
    defaultEnvironment = Environments.Development;
}
#endif
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = defaultEnvironment
});
var connectionString = builder.Configuration.GetConnectionString("NovelDb");
if (string.IsNullOrWhiteSpace(connectionString) && builder.Environment.IsDevelopment())
{
    // Local development uses Windows Authentication, so no database credential is needed.
    connectionString = new SqlConnectionStringBuilder
    {
        DataSource = builder.Configuration["LocalDatabase:Server"] ?? @"lpc:.\SQLEXPRESS",
        InitialCatalog = builder.Configuration["LocalDatabase:Name"] ?? "NovelReadingPlatform",
        IntegratedSecurity = true,
        TrustServerCertificate = true
    }.ConnectionString;
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"Thiếu cấu hình ConnectionStrings:NovelDb (môi trường {builder.Environment.EnvironmentName}). "
        + "Khi chạy local, chọn profile https và đặt ConnectionStrings:NovelDb trong Manage User Secrets. "
        + "Khi triển khai, đặt biến môi trường ConnectionStrings__NovelDb.");
}

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/Index";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (int.TryParse(context.Principal?.FindFirst("uid")?.Value, out var userId))
            {
                var user = await db.Users
                    .Where(user => user.Id == userId && user.Status == "Active")
                    .Select(user => new { Role = user.Role.Name, user.PasswordHash, user.SessionVersion })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                if (user is not null && context.Principal!.IsInRole(user.Role)
                    && context.Principal.FindFirst("session_stamp")?.Value == SeedData.SessionStamp(user.PasswordHash)
                    && context.Principal.FindFirst("session_version")?.Value == user.SessionVersion.ToString())
                {
                    return;
                }
            }

            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        await SeedData.InitializeAsync(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            app.Configuration);
    }
    catch (Exception exception)
    {
        app.Logger.LogCritical(exception, "Database initialization failed");
        throw;
    }
}

if (args.Contains("--seed-only", StringComparer.Ordinal))
{
    app.Logger.LogInformation("Database initialization completed");
    return;
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerPathFeature>();
    app.Logger.LogError(error?.Error, "Unhandled exception for {Path}", error?.Path);
    if (error?.Path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) == true)
    {
        await Results.Problem(statusCode: 500, title: "Không thể xử lý yêu cầu.").ExecuteAsync(context);
        return;
    }

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync("""
        <!doctype html><html lang="vi"><meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Đã xảy ra lỗi</title>
        <h1>Đã xảy ra lỗi</h1><p>Vui lòng thử lại.</p><a href="/">Về trang chủ</a></html>
        """);
}));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("vi-VN")
    .AddSupportedCultures("vi-VN")
    .AddSupportedUICultures("vi-VN"));
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
