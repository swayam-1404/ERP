using Microsoft.EntityFrameworkCore;
using Npgsql;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;
public record FacultyRequest(string FullName,string Email,string Designation,Guid? DepartmentId,int Version=1);
public static class FacultyEndpoints
{
    public static void MapFaculty(this RouteGroupBuilder group,WebApplication app)
    {
        group.MapGet("/faculty",async(Guid institutionId,string? search,int? page,ErpDbContext db)=>
        {
            var p=Math.Clamp(page??1,1,10000);var query=db.Faculty.Where(x=>x.InstitutionId==institutionId&&!x.IsArchived);if(!string.IsNullOrWhiteSpace(search)){if(search.Length>100)return Results.Problem("Search too long.",statusCode:400);query=query.Where(x=>EF.Functions.ILike(x.FullName,"%"+search+"%"));}
            return Results.Ok(new{items=await query.OrderBy(x=>x.FullName).Skip((p-1)*20).Take(20).ToListAsync(),total=await query.CountAsync(),page=p,pageSize=20});
        });
        group.MapPost("/faculty",async(Guid institutionId,FacultyRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(string.IsNullOrWhiteSpace(r.FullName)||r.FullName.Length>120||!StudentEndpoints.EmailValid(r.Email)||string.IsNullOrWhiteSpace(r.Designation)||r.Designation.Length>120)return Results.Problem("Enter a valid name, email and designation (up to 120 characters).",statusCode:400);
            if(r.DepartmentId!=null&&!await db.Departments.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.DepartmentId))return Results.Problem("Department must belong to this institution.",statusCode:400);
            var faculty=new Faculty{InstitutionId=institutionId,FullName=r.FullName.Trim(),Email=r.Email,Designation=r.Designation.Trim(),DepartmentId=r.DepartmentId,IsDemo=app.Environment.IsDevelopment()};db.Faculty.Add(faculty);db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="Faculty.Created",Resource=faculty.Id.ToString(),Details="Faculty profile created. No user permissions granted."});
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Faculty email already exists.",statusCode:409);}return Results.Created($"/api/v1/institutions/{institutionId}/faculty",faculty);
        }).RequireAuthorization("Manage");
    }
}
