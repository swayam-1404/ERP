using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;
public record GradingPolicyRequest(string Name,Guid ProgrammeId,Guid AcademicSessionId,string SourceAcademicYear,string SourceReference,string FailGrade,GradeBand[] Bands);
public record AssessmentRequest(string Name,Guid SubjectId,Guid GradingPolicyId,Guid FacultyId,string Section,DateOnly ScheduledOn,string ConfigurationReference,decimal Credits,decimal MaximumInternal,decimal MaximumExternal,decimal PassInternal,decimal PassExternal,decimal PassTotal);
public record MarkInput(Guid StudentId,string Attendance,decimal? InternalMark,decimal? ExternalMark);
public record MarksRequest(int Version,MarkInput[] Items);
public static partial class ExaminationEndpoints
{
    private static bool Manager(HttpContext ctx)=>ctx.User.IsInRole("CentralAdministrator")||ctx.User.IsInRole("InstitutionAdministrator");
    private static bool Text(string? s,int min,int max)=>s!=null&&s.Trim().Length>=min&&s.Length<=max;
    private static bool Precise(decimal d)=>decimal.Round(d,2)==d;
    private static bool GradeCode(string? s)=>Text(s,1,8)&&s!.All(c=>char.IsAsciiLetterOrDigit(c)||c is '+' or '-');
    private static Task<bool> Allowed(HttpContext ctx,ErpDbContext db,ExamAssessment e)=>Manager(ctx)?Task.FromResult(true):db.Faculty.AnyAsync(f=>f.InstitutionId==e.InstitutionId&&f.Id==e.FacultyId&&f.UserId==ctx.Actor()&&!f.IsArchived);
    private static async Task<ExamAssessment?> Lock(ErpDbContext db,Guid institution,Guid id)=>(await db.ExamAssessments.FromSqlInterpolated($"SELECT * FROM \"ExamAssessments\" WHERE \"InstitutionId\"={institution} AND \"Id\"={id} FOR UPDATE").ToListAsync()).SingleOrDefault();
    private static void Audit(ErpDbContext db,HttpContext ctx,Guid institution,string action,Guid id,string note)=>db.AuditLogs.Add(new AuditLog{InstitutionId=institution,ActorId=ctx.Actor(),Action="Examination."+action,Resource=id.ToString(),Details=note});
    public static void MapExaminations(this RouteGroupBuilder group,WebApplication app)
    {
        var routes=group.MapGroup("/examinations").RequireAuthorization("Teaching");
        routes.MapGet("/policies",async(Guid institutionId,string? status,int? page,int? pageSize,HttpContext ctx,ErpDbContext db)=>
        {
            if(status!=null&&status is not("Draft" or "Approved"))return Results.Problem("Invalid policy status.",statusCode:400);
            var p=Math.Clamp(page??1,1,10000);var size=Math.Clamp(pageSize??25,1,100);var query=db.GradingPolicies.Where(x=>x.InstitutionId==institutionId&&(Manager(ctx)||x.Status=="Approved")&&(status==null||x.Status==status));return Results.Ok(new{items=await query.OrderBy(x=>x.Name).ThenBy(x=>x.Id).Skip((p-1)*size).Take(size).ToListAsync(),total=await query.CountAsync(),page=p});
        });
        routes.MapPost("/policies",async(Guid institutionId,GradingPolicyRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Name,3,120)||!Text(r.SourceReference,3,500)||!Text(r.SourceAcademicYear,4,20)||!GradeCode(r.FailGrade)||r.Bands==null||r.Bands.Length is <1 or >20||r.Bands.Any(b=>b==null||!GradeCode(b.Grade)||b.MinimumPercent is <0 or >100||b.Points is <0 or >10||!Precise(b.MinimumPercent)||!Precise(b.Points)))return Results.Problem("Provide a source-backed policy, fail grade and 1–20 valid grade bands (minimum percent 0–100, points 0–10).",statusCode:400);
            var bands=r.Bands.OrderBy(x=>x.MinimumPercent).ToArray();
            if(bands[0].MinimumPercent!=0||bands.Select(x=>x.MinimumPercent).Distinct().Count()!=bands.Length||bands.Select(x=>x.Grade.ToUpperInvariant()).Distinct().Count()!=bands.Length||bands.Zip(bands.Skip(1)).Any(pair=>pair.First.Points>pair.Second.Points))return Results.Problem("Grade bands must start at 0%, have distinct minimum percentages/grades and non-decreasing grade points.",statusCode:400);
            if(!await db.Programmes.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.ProgrammeId)||!await db.AcademicSessions.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.AcademicSessionId))return Results.Problem("Programme and session must belong to this institution.",statusCode:400);
            var policy=new GradingPolicy{InstitutionId=institutionId,Name=r.Name.Trim(),ProgrammeId=r.ProgrammeId,AcademicSessionId=r.AcademicSessionId,SourceAcademicYear=r.SourceAcademicYear.Trim(),SourceReference=r.SourceReference.Trim(),FailGrade=r.FailGrade.ToUpperInvariant(),BandsJson=JsonSerializer.Serialize(bands.Select(b=>b with{Grade=b.Grade.ToUpperInvariant()})),CreatedBy=ctx.Actor(),IsDemo=app.Environment.IsDevelopment()};db.GradingPolicies.Add(policy);Audit(db,ctx,institutionId,"PolicyCreated",policy.Id,"Draft grading policy; two-decimal percentage/GPA, midpoint away from zero.");await db.SaveChangesAsync();return Results.Created($"/api/v1/institutions/{institutionId}/examinations/policies",policy);
        }).RequireAuthorization("Manage");
        routes.MapPost("/policies/{id:guid}/approve",async(Guid institutionId,Guid id,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Reason,3,500))return Results.Problem("Provide a policy approval note.",statusCode:400);var policy=await db.GradingPolicies.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(policy==null)return Results.NotFound();if(policy.Status!="Draft"||policy.Version!=r.Version)return Results.Problem("Policy changed or already approved.",statusCode:409);if(policy.CreatedBy==ctx.Actor())return Results.Problem("Another administrator must approve the grading policy.",statusCode:403);
            var session=await db.AcademicSessions.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==policy.AcademicSessionId);if(policy.SourceAcademicYear.Replace('–','-')!=session.Name.Replace('–','-'))return Results.Problem("Policy source year must match the academic session.",statusCode:409);
            policy.Status="Approved";policy.ApprovedBy=ctx.Actor();policy.ApprovalNote=r.Reason.Trim();policy.Version++;Audit(db,ctx,institutionId,"PolicyApproved",id,"Referenced grading/rounding policy independently approved.");try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("Policy changed. Reload before approving.",statusCode:409);}return Results.Ok(policy);
        }).RequireAuthorization("Manage");
        routes.MapGet("",async(Guid institutionId,int? page,HttpContext ctx,ErpDbContext db)=>
        {
            var p=Math.Clamp(page??1,1,10000);var query=db.ExamAssessments.Where(x=>x.InstitutionId==institutionId&&(Manager(ctx)||db.Faculty.Any(f=>f.InstitutionId==institutionId&&f.Id==x.FacultyId&&f.UserId==ctx.Actor()&&!f.IsArchived)));return Results.Ok(new{items=await query.OrderByDescending(x=>x.ScheduledOn).ThenBy(x=>x.Id).Skip((p-1)*25).Take(25).ToListAsync(),total=await query.CountAsync(),page=p});
        });
        routes.MapPost("",async(Guid institutionId,AssessmentRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            var amounts=new[]{r.Credits,r.MaximumInternal,r.MaximumExternal,r.PassInternal,r.PassExternal,r.PassTotal};
            if(!Text(r.Name,3,120)||!Text(r.Section,1,12)||!Text(r.ConfigurationReference,3,500)||r.ScheduledOn.Year is <2000 or >2100||amounts.Any(x=>!Precise(x))||r.Credits is <=0 or >30||r.MaximumInternal is <0 or >1000||r.MaximumExternal is <=0 or >1000||r.PassInternal<0||r.PassInternal>r.MaximumInternal||r.PassExternal<0||r.PassExternal>r.MaximumExternal||r.PassTotal<0||r.PassTotal>r.MaximumInternal+r.MaximumExternal)return Results.Problem("Provide the assessment, verified configuration reference, section, credits and valid component/pass limits with at most two decimals.",statusCode:400);
            var subject=await db.Subjects.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==r.SubjectId);var policy=await db.GradingPolicies.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==r.GradingPolicyId&&x.Status=="Approved");
            if(subject==null||policy==null||subject.ProgrammeId!=policy.ProgrammeId||subject.AcademicSessionId!=policy.AcademicSessionId||policy.IsDemo!=app.Environment.IsDevelopment()||!await db.Faculty.AnyAsync(f=>f.InstitutionId==institutionId&&f.Id==r.FacultyId&&!f.IsArchived))return Results.Problem("Use an approved matching programme/session grading policy and active faculty within this institution.",statusCode:400);
            var section=r.Section.Trim().ToUpperInvariant();var roster=await(from e in db.Enrollments join s in db.Students on new{e.InstitutionId,Id=e.StudentId} equals new{s.InstitutionId,s.Id} where e.InstitutionId==institutionId&&e.ProgrammeId==subject.ProgrammeId&&e.AcademicSessionId==subject.AcademicSessionId&&e.Semester==subject.Semester&&e.Section==section&&(s.Status=="Active"||s.Status=="NewlyAdmitted") select new{e.Id,e.StudentId}).ToListAsync();if(roster.Count==0||roster.Count>1000)return Results.Problem("Assessment needs an eligible saved roster of 1–1000 students.",statusCode:400);
            var exam=new ExamAssessment{InstitutionId=institutionId,Name=r.Name.Trim(),SubjectId=subject.Id,GradingPolicyId=policy.Id,FacultyId=r.FacultyId,Section=section,ScheduledOn=r.ScheduledOn,ConfigurationReference=r.ConfigurationReference.Trim(),Credits=r.Credits,MaximumInternal=r.MaximumInternal,MaximumExternal=r.MaximumExternal,PassInternal=r.PassInternal,PassExternal=r.PassExternal,PassTotal=r.PassTotal,CreatedBy=ctx.Actor(),IsDemo=app.Environment.IsDevelopment()};db.ExamAssessments.Add(exam);foreach(var row in roster)db.ExamMarks.Add(new ExamMark{InstitutionId=institutionId,AssessmentId=exam.Id,SubjectId=subject.Id,StudentId=row.StudentId,EnrollmentId=row.Id});Audit(db,ctx,institutionId,"AssessmentCreated",exam.Id,$"Saved roster of {roster.Count}; no marks assumed.");try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("This subject/section already has an assessment. Retakes require a separate reviewed revision workflow.",statusCode:409);}return Results.Created($"/api/v1/institutions/{institutionId}/examinations/{exam.Id}",exam);
        }).RequireAuthorization("Manage");
        routes.MapGet("/{id:guid}",async(Guid institutionId,Guid id,HttpContext ctx,ErpDbContext db)=>
        {
            var exam=await db.ExamAssessments.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(exam==null)return Results.NotFound();if(!await Allowed(ctx,db,exam))return Results.Problem("Assessment is not assigned to you.",statusCode:403);
            var marks=await(from mark in db.ExamMarks join s in db.Students on new{mark.InstitutionId,Id=mark.StudentId} equals new{s.InstitutionId,s.Id} where mark.InstitutionId==institutionId&&mark.AssessmentId==id orderby s.StudentNumber select new{mark.StudentId,s.StudentNumber,s.FullName,mark.Attendance,mark.InternalMark,mark.ExternalMark,mark.Total,mark.Percentage,mark.Grade,mark.GradePoints,mark.IsPass}).ToListAsync();return Results.Ok(new{assessment=exam,marks});
        });
        MapMarks(routes);
        group.MapGet("/students/{studentId:guid}/results",async(Guid institutionId,Guid studentId,ErpDbContext db)=>!await db.Students.AnyAsync(s=>s.InstitutionId==institutionId&&s.Id==studentId)?Results.NotFound():Results.Ok(await Transcript(db,institutionId,studentId))).RequireAuthorization("Manage");
        group.MapGet("/portal/students/{studentId:guid}/results",async(Guid institutionId,Guid studentId,HttpContext ctx,ErpDbContext db)=>!await PortalEndpoints.Accessible(db,ctx,institutionId).AnyAsync(s=>s.Id==studentId)?Results.NotFound():Results.Ok(await Transcript(db,institutionId,studentId))).RequireAuthorization(p=>p.RequireRole("Student","Guardian"));
    }
}
