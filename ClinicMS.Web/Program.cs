using ClinicMS.Web.Data;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Mocks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ClinicMsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Every [FromBody] request record with an enum field (DiscountType, ChannelType, PatientGender,
// PricingModelType, ...) is posted from JS as the enum's string name, matching how ViewJson and
// ApiClientBase already serialize enums for outgoing traffic -- without this, model binding would
// reject those strings since System.Text.Json expects enums as numbers by default.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ClinicMS.API is not wired up yet -- every API client is backed by an in-memory mock (see
// Services/Api/Mocks) so the frontend can be designed and clicked through end-to-end with no
// backend running. Swap these for the AddHttpClient<T,U> registrations once the API is ready.
builder.Services.AddSingleton<IAuthApiClient, MockAuthApiClient>();
builder.Services.AddSingleton<IUsersApiClient, MockUsersApiClient>();
builder.Services.AddSingleton<IRolesApiClient, MockRolesApiClient>();
builder.Services.AddSingleton<ISettingsApiClient, MockSettingsApiClient>();
builder.Services.AddSingleton<ISmsApiClient, MockSmsApiClient>();
builder.Services.AddSingleton<IExpensesApiClient, MockExpensesApiClient>();
builder.Services.AddSingleton<IPaymentsApiClient, MockPaymentsApiClient>();
builder.Services.AddSingleton<INotificationsApiClient, MockNotificationsApiClient>();
builder.Services.AddSingleton<IAuditApiClient, MockAuditApiClient>();
builder.Services.AddSingleton<IDashboardApiClient, MockDashboardApiClient>();
builder.Services.AddSingleton<IPatientsApiClient, MockPatientsApiClient>();
builder.Services.AddSingleton<IMedicalServicesApiClient, MockMedicalServicesApiClient>();
builder.Services.AddSingleton<ISupplyChainApiClient, MockSupplyChainApiClient>();

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
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
