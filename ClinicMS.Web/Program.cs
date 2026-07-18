using ClinicMS.Web.Services.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

builder.Services.AddTransient<AuthTokenHandler>();
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IUsersApiClient, UsersApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IRolesApiClient, RolesApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<ISettingsApiClient, SettingsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<ISmsApiClient, SmsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IExpensesApiClient, ExpensesApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IPaymentsApiClient, PaymentsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<INotificationsApiClient, NotificationsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IAuditApiClient, AuditApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IDashboardApiClient, DashboardApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();

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
