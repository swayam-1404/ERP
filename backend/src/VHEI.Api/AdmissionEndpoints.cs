using Microsoft.EntityFrameworkCore;
using Npgsql;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;

public record AdmissionRequest(string ApplicationNumber,string FullName,string Email,Guid ProgrammeId,Guid AcademicSessionId,string ExternalReference,int Version=1);
public record AdmissionTransition(int Version,string Action,string Reason,string PolicyReference="",string EligibilityNote="",string DocumentReference="");

public static class AdmissionEndpoints
{
    public static readonly string[] Statuses=["Draft","Submitted","Reviewed","Rejected","Cancelled"];
    private static bool Text(string? s,int min,int max)=>s!=null&&s.Trim().Length>=min&&s.Length<=max;
    private static void Event(ErpDbContext db,AdmissionApplication a,Guid actor,string action,string note)
    {
        db.AdmissionEvents.Add(new AdmissionEvent{InstitutionId=a.InstitutionId,ApplicationId=a.Id,ActorId=actor,Status=a.Status,Note=note});
        db.AuditLogs.Add(new AuditLog{InstitutionId=a.InstitutionId,ActorId=actor,Action="Admission."+action,Resource=a.Id.ToString(),Details=$"Application version {a.Version}; status {a.Status}."});
    }
    public static void MapAdmissions(this RouteGroupBuilder group,WebApplication app)
    {
        var routes=group.MapGroup("/admissions").RequireAuthorization("Manage");
        routes.MapGet("",async(Guid institutionId,string? search,string? status,int? page,ErpDbContext db)=>
        {
            if(search?.Length>100||status!=null&&!Statuses.Contains(status))return Results.Problem("Invalid search or application status.",statusCode:400);
            var query=db.AdmissionApplications.Where(x=>x.InstitutionId==institutionId);
            if(!string.IsNullOrWhiteSpace(search)){var term="%"+search.Trim()+"%";query=query.Where(x=>EF.Functions.ILike(x.FullName,term)||EF.Functions.ILike(x.ApplicationNumber,term)||EF.Functions.ILike(x.ExternalReference,term));}
            if(status!=null)query=query.Where(x=>x.Status==status);
            var p=Math.Clamp(page??1,1,10000);
            return Results.Ok(new{items=await query.OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id).Skip((p-1)*25).Take(25).ToListAsync(),total=await query.CountAsync(),page=p,pageSize=25});
        });
        routes.MapGet("/summary",async(Guid institutionId,ErpDbContext db)=>Results.Ok(await db.AdmissionApplications.Where(x=>x.InstitutionId==institutionId).GroupBy(x=>x.Status).Select(x=>new{status=x.Key,count=x.Count()}).ToListAsync()));
        routes.MapGet("/{id:guid}",async(Guid institutionId,Guid id,ErpDbContext db)=>
        {
            var a=await db.AdmissionApplications.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);
            return a==null?Results.NotFound():Results.Ok(new{application=a,history=await db.AdmissionEvents.Where(x=>x.InstitutionId==institutionId&&x.ApplicationId==id).OrderBy(x=>x.OccurredAt).ToListAsync()});
        });
        routes.MapPost("",async(Guid institutionId,AdmissionRequest r,HttpContext ctx,ErpDbContext db)=>await Save(institutionId,null,r,ctx,db,app.Environment.IsDevelopment()));
        routes.MapPut("/{id:guid}",async(Guid institutionId,Guid id,AdmissionRequest r,HttpContext ctx,ErpDbContext db)=>await Save(institutionId,id,r,ctx,db,app.Environment.IsDevelopment()));
        routes.MapPost("/{id:guid}/transition",async(Guid institutionId,Guid id,AdmissionTransition r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Reason,3,1000))return Results.Problem("Enter an action note of 3–1000 characters.",statusCode:400);
            var a=await db.AdmissionApplications.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);
            if(a==null)return Results.NotFound();
            if(a.Version!=r.Version)return Results.Problem("Application changed. Reload before continuing.",statusCode:409);
            switch(r.Action)
            {
                case "Submit" when a.Status=="Draft":a.Status="Submitted";a.SubmittedBy=ctx.Actor();break;
                case "Review" or "Reject" when a.Status=="Submitted":
                    if(a.SubmittedBy==ctx.Actor())return Results.Problem("A different administrator must review the submitted application.",statusCode:403);
                    if(r.Action=="Review"&&(!Text(r.PolicyReference,3,500)||!Text(r.EligibilityNote,3,1000)||!Text(r.DocumentReference,3,1000)))return Results.Problem("Record the applicable policy reference, eligibility evidence and verified document reference before completing review.",statusCode:400);
                    a.Status=r.Action=="Review"?"Reviewed":"Rejected";a.ReviewedBy=ctx.Actor();a.ReviewNote=r.Reason.Trim();
                    if(r.Action=="Review"){a.PolicyReference=r.PolicyReference.Trim();a.EligibilityNote=r.EligibilityNote.Trim();a.DocumentReference=r.DocumentReference.Trim();}
                    break;
                case "Cancel" when a.Status is "Draft" or "Submitted" or "Reviewed":a.Status="Cancelled";break;
                default:return Results.Problem("This action is unavailable for the current application status.",statusCode:409);
            }
            a.Version++;Event(db,a,ctx.Actor(),r.Action,r.Reason.Trim());
            try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("Application changed. Reload before continuing.",statusCode:409);}
            return Results.Ok(a);
        });
    }
    private static async Task<IResult> Save(Guid institutionId,Guid? id,AdmissionRequest r,HttpContext ctx,ErpDbContext db,bool demo)
    {
        if(!Text(r.ApplicationNumber,1,40)||!r.ApplicationNumber.All(c=>char.IsLetterOrDigit(c)||c is '-' or '/')||!Text(r.FullName,2,120)||!StudentEndpoints.EmailValid(r.Email)||r.ExternalReference==null||r.ExternalReference.Length>120)return Results.Problem("Enter a valid application number (letters, digits, hyphens or slashes), name, email and external reference up to 120 characters.",statusCode:400);
        if(!await db.Programmes.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.ProgrammeId)||!await db.AcademicSessions.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.AcademicSessionId))return Results.Problem("Programme and session must belong to this institution.",statusCode:400);
        AdmissionApplication a;
        if(id!=null){var found=await db.AdmissionApplications.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(found==null)return Results.NotFound();a=found;if(a.Version!=r.Version||a.Status!="Draft")return Results.Problem("Only the current draft can be edited. Reload the application.",statusCode:409);a.Version++;}
        else{a=new AdmissionApplication{InstitutionId=institutionId,CreatedBy=ctx.Actor(),IsDemo=demo};db.AdmissionApplications.Add(a);}
        a.ApplicationNumber=r.ApplicationNumber.Trim().ToUpperInvariant();a.FullName=r.FullName.Trim();a.Email=r.Email.Trim();a.ProgrammeId=r.ProgrammeId;a.AcademicSessionId=r.AcademicSessionId;a.ExternalReference=r.ExternalReference.Trim();
        Event(db,a,ctx.Actor(),id==null?"Created":"Updated",id==null?"Application registered as draft.":"Draft details updated.");
        try{await db.SaveChangesAsync();}
        catch(DbUpdateConcurrencyException){return Results.Problem("Application changed. Reload before saving.",statusCode:409);}
        catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Application number already exists in this institution.",statusCode:409);}
        return id==null?Results.Created($"/api/v1/institutions/{institutionId}/admissions/{a.Id}",a):Results.Ok(a);
    }
}
