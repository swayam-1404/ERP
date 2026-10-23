using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using VHEI.Infrastructure;
using VHEI.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
builder.Services.AddProblemDetails();
builder.Services.AddDataProtection().SetApplicationName("VHEI.ERP").PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeyPath"]??Path.Combine(AppContext.BaseDirectory,"keys")));
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ErpDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Erp") ?? throw new InvalidOperationException("ConnectionStrings__Erp must be configured.")));
builder.Services.AddOpenTelemetry().WithTracing(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()).WithMetrics(x => x.AddAspNetCoreInstrumentation());
builder.Services.AddIdentity<ErpUser,IdentityRole<Guid>>(options=>{ options.Password.RequiredLength=12;options.User.RequireUniqueEmail=true;options.Lockout.MaxFailedAccessAttempts=5;options.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15); }).AddEntityFrameworkStores<ErpDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options=>{options.Cookie.Name="vhei.session";options.Cookie.HttpOnly=true;options.Cookie.SameSite=SameSiteMode.Strict;options.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;options.ExpireTimeSpan=TimeSpan.FromMinutes(30);options.SlidingExpiration=true;options.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};options.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};});
builder.Services.AddAntiforgery(o=>{o.HeaderName="X-CSRF-TOKEN";o.Cookie.Name="vhei.csrf";o.Cookie.SameSite=SameSiteMode.Strict;o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;});
builder.Services.AddAuthorization(options=>{
    options.AddPolicy("Workspace",policy=>{policy.RequireAuthenticatedUser();policy.RequireAssertion(c=>appIsDev()||!c.User.IsInRole("CentralAdministrator")&&!c.User.IsInRole("InstitutionAdministrator")||c.User.HasClaim("amr","mfa"));});
    options.AddPolicy("Manage",policy=>{policy.RequireRole("CentralAdministrator","InstitutionAdministrator");policy.RequireAssertion(c=>appIsDev()||c.User.HasClaim("amr","mfa"));});
    options.AddPolicy("Teaching",policy=>policy.RequireRole("CentralAdministrator","InstitutionAdministrator","Faculty"));
});
bool appIsDev()=>builder.Environment.IsDevelopment();
builder.Services.AddRateLimiter(options=>{options.RejectionStatusCode=429;options.AddPolicy("verification",ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=30,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));options.AddPolicy("login",ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=Math.Clamp(builder.Configuration.GetValue<int?>("RateLimiting:LoginPermitLimit")??10,1,50),Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
var app = builder.Build();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async(ctx,next)=>{
    if(ctx.Request.Path.StartsWithSegments("/api")&&ctx.Request.Path.Value?.Contains("/certificates")==true)ctx.Response.Headers.CacheControl="no-store";
    if(ctx.Request.Path.StartsWithSegments("/api")&&ctx.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
    {
        try {await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx);} catch(AntiforgeryValidationException){await Results.Problem("Antiforgery token is missing or invalid.",statusCode:400).ExecuteAsync(ctx);return;}
    }
    await next();
});
var seedMode=builder.Configuration["Seed:Mode"]??"None";
if(!app.Environment.IsDevelopment()&&seedMode=="Demo")throw new InvalidOperationException("Synthetic seed mode is forbidden outside Development.");
if(seedMode is "Demo" or "Reference")
{
    using var scope=app.Services.CreateScope();
    var db=scope.ServiceProvider.GetRequiredService<ErpDbContext>();
    await db.Database.MigrateAsync();
    await ReferenceSeed.RunAsync(db,Path.Combine(AppContext.BaseDirectory,"reference-data.json"),seedMode=="Demo",scope.ServiceProvider.GetRequiredService<UserManager<ErpUser>>(),scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),builder.Configuration["Seed:AdminPassword"]);
    if(seedMode=="Demo")await DemoSeed.RunAsync(db);
    if(seedMode=="Demo")await AcademicDemoSeed.RunAsync(db);
    if(seedMode=="Demo")await PortalDemoSeed.RunAsync(db);
}
app.MapAuth();
var institutionRoutes=app.MapCore();
institutionRoutes.MapStudents(app);
institutionRoutes.MapAdmissions(app);
institutionRoutes.MapPortal(app);
institutionRoutes.MapExaminations(app);
institutionRoutes.MapCertificates(app);
institutionRoutes.MapAdmissionFinance(app);
institutionRoutes.MapFaculty(app);
institutionRoutes.MapAcademics(app);
institutionRoutes.MapRecurringTimetable(app);
app.MapGet("/api/v1/overview",async(HttpContext ctx,ErpDbContext db)=>{
    var assigned=await db.InstitutionAssignments.Where(x=>x.UserId==ctx.Actor()).Select(x=>x.InstitutionId).ToListAsync();
    return Results.Ok(new{students=await db.Students.CountAsync(x=>assigned.Contains(x.InstitutionId)&&x.Status!="Archived"),institutions=assigned.Count,faculty=await db.Faculty.CountAsync(x=>assigned.Contains(x.InstitutionId)&&!x.IsArchived),byInstitution=await db.Institutions.Where(x=>assigned.Contains(x.Id)).OrderBy(x=>x.Code).Select(x=>new{name=x.Code,students=db.Students.Count(s=>s.InstitutionId==x.Id&&s.Status!="Archived")}).ToListAsync()});
}).RequireAuthorization("Manage");
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy", service = "VHEI ERP API" }));
app.MapGet("/health/ready", async (ErpDbContext db, CancellationToken ct) =>
{
    try { return await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ready", database = "connected" }) : Results.Problem("Database unavailable", statusCode: 503); }
    catch { return Results.Problem("Database unavailable", statusCode: 503); }
});
app.MapGet("/api/v1/system", () => Results.Ok(new { name = "VHEI ERP", version = "0.1.0", environment = app.Environment.EnvironmentName, mode = app.Environment.IsDevelopment() ? "DEMO" : "PRODUCTION", milestone = "Foundation", modules = 16 }));
app.Run();
public partial class Program { }
