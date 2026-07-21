using ClinicMS.Web.Data;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ClinicMsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Every [FromBody] request record with an enum field (DiscountType, ChannelType, PatientGender,
// PricingModelType, ...) is posted from JS as the enum's string name, matching how ViewJson and
// ApiClientBase already serialize enums for outgoing traffic -- without this, model binding would
// reject those strings since System.Text.Json expects enums as numbers by default.
//
// AutoValidateAntiforgeryTokenAttribute enforces CSRF protection on every unsafe-method request
// (POST/PUT/DELETE/PATCH) app-wide. Every page layout renders a __RequestVerificationToken-backed
// cookie plus a `csrf-token` meta tag; wwwroot/scripts/shared/csrf.js reads that meta tag and
// attaches it as the X-CSRF-TOKEN header to every fetch() call so none of the existing Feature
// scripts had to change. The two plain (non-fetch) logout <form> posts carry the token as a
// hidden field via @Html.AntiForgeryToken() instead.
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // SameAsRequest (not Always) so the cookie still works over the plain-http local dev profile --
    // it becomes Secure automatically once the app is actually served over https in production.
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// IP-partitioned limiters on the credential/code-guessing surfaces (login password, OTP, reset
// code) -- these are the endpoints an attacker would hammer to brute-force their way in.
// QueueLimit 0 means excess requests are rejected immediately (429) rather than queued/delayed.
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many attempts. Please wait a few minutes and try again." },
            cancellationToken);
    };

    static string PartitionKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));

    options.AddPolicy("otp", context => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));

    options.AddPolicy("passwordReset", context => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
});

// ClinicMS.API is not wired up -- every feature is backed directly by the real clinicMS.vera
// database via ClinicMsDbContext instead (Services/Api/Db). All these clients read/write a scoped
// DbContext, so they're registered scoped too.
builder.Services.AddScoped<IPatientsApiClient, DbPatientsApiClient>();
builder.Services.AddScoped<ISettingsApiClient, DbSettingsApiClient>();
builder.Services.AddScoped<ISmsApiClient, DbSmsApiClient>();
builder.Services.AddScoped<IMedicalServicesApiClient, DbMedicalServicesApiClient>();
builder.Services.AddScoped<IExpensesApiClient, DbExpensesApiClient>();
builder.Services.AddScoped<ISupplyChainApiClient, DbSupplyChainApiClient>();
builder.Services.AddScoped<IUsersApiClient, DbUsersApiClient>();
builder.Services.AddScoped<IRolesApiClient, DbRolesApiClient>();
builder.Services.AddScoped<IAuditApiClient, DbAuditApiClient>();
builder.Services.AddScoped<IReportsApiClient, DbReportsApiClient>();
builder.Services.AddScoped<IAuthApiClient, DbAuthApiClient>();
builder.Services.AddScoped<IDashboardApiClient, DbDashboardApiClient>();
builder.Services.AddScoped<IPaymentsApiClient, DbPaymentsApiClient>();
builder.Services.AddScoped<INotificationsApiClient, DbNotificationsApiClient>();

var app = builder.Build();

// Development gets the full diagnostic page; everything else gets the same friendly ServerError
// view already used for explicit error status codes below, plus HSTS/HTTPS enforcement -- both of
// which stay off in Development so the plain-http local dev profile (.claude/launch.json) keeps
// working without redirect-port warnings or a certificate.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/StatusCode?code=500");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/Home/Index");
        return;
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
