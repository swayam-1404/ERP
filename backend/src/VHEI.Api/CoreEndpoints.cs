using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
using VHEI.Infrastructure;
using Npgsql;
namespace VHEI.Api;
public record DepartmentRequest(string Name);
public static class CoreEndpoints
{
    public static RouteGroupBuilder MapCore(this WebApplication app)
    {
        app.MapGet("/api/v1/institutions",async(HttpContext ctx,ErpDbContext db)=>Results.Ok(await db.Institutions.Where(i=>db.InstitutionAssignments.Any(a=>a.InstitutionId==i.Id&&a.UserId==ctx.Actor())).OrderBy(i=>i.Code).ToListAsync())).RequireAuthorization("Workspace");
        var group=app.MapGroup("/api/v1/institutions/{institutionId:guid}").RequireAuthorization("Workspace").AddEndpointFilter<InstitutionAccessFilter>();
        group.MapGet("/reference",async(Guid institutionId,ErpDbContext db)=>Results.Ok(new {institution=await db.Institutions.SingleAsync(i=>i.Id==institutionId),departments=await db.Departments.Where(x=>x.InstitutionId==institutionId).OrderBy(x=>x.Name).ToListAsync(),programmes=await db.Programmes.Where(x=>x.InstitutionId==institutionId).OrderBy(x=>x.Name).ToListAsync(),sessions=await db.AcademicSessions.Where(x=>x.InstitutionId==institutionId).OrderBy(x=>x.Name).ToListAsync()}));
        group.MapGet("/dashboard",async(Guid institutionId,ErpDbContext db)=>Results.Ok(new {departmentCount=await db.Departments.CountAsync(x=>x.InstitutionId==institutionId),programmeCount=await db.Programmes.CountAsync(x=>x.InstitutionId==institutionId),auditCount=await db.AuditLogs.CountAsync(x=>x.InstitutionId==institutionId)}));
        group.MapGet("/audit",async(Guid institutionId,int? page,ErpDbContext db)=>Results.Ok(new {items=await db.AuditLogs.Where(x=>x.InstitutionId==institutionId).OrderByDescending(x=>x.OccurredAt).Skip((Math.Clamp(page??1,1,10000)-1)*25).Take(25).ToListAsync(),total=await db.AuditLogs.CountAsync(x=>x.InstitutionId==institutionId)})).RequireAuthorization("Manage");
        group.MapPost("/departments",async(Guid institutionId,DepartmentRequest request,HttpContext ctx,ErpDbContext db)=>
        {
            var name=request.Name?.Trim()??"";
            if(name.Length<2||name.Length>120) return Results.ValidationProblem(new Dictionary<string,string[]>{["name"]=["Name must be 2–120 characters."]});
            if(await db.Departments.AnyAsync(x=>x.InstitutionId==institutionId&&x.Name==name))return Results.Problem("Department already exists.",statusCode:409);
            var department=new Department{InstitutionId=institutionId,Name=name,VerificationStatus="UNVERIFIED",SourceUrl=""};
            db.Departments.Add(department);
            db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="Department.Created",Resource=department.Id.ToString(),Details="Locally configured; not official-verified."});
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Department already exists.",statusCode:409);}
            return Results.Created($"/api/v1/institutions/{institutionId}/reference",department);
        }).RequireAuthorization("Manage");
        return group;
    }
}
