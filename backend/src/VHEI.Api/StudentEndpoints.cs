using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;
public record StudentRequest(string StudentNumber,string FullName,string Email,string Status,Guid ProgrammeId,Guid AcademicSessionId,int BatchYear,int Semester,string Section,string GuardianName,string GuardianEmail,int Version=1);
public record ArchiveRequest(int Version,string Reason);
public static class StudentEndpoints
{
    public static readonly string[] Statuses=["Active","NewlyAdmitted","Graduated","Alumni","Archived"];
    public static bool EmailValid(string? email)=>email!=null&&email.Length<=254&&MailAddress.TryCreate(email,out var parsed)&&parsed.Address==email;
    private static Dictionary<string,string[]> Validate(StudentRequest r)
    {
        var errors=new Dictionary<string,string[]>();
        if(string.IsNullOrWhiteSpace(r.FullName)||r.FullName.Trim().Length is <2 or >120)errors["fullName"]=["Name must be 2–120 characters."];
        if(string.IsNullOrWhiteSpace(r.StudentNumber)||r.StudentNumber.Length>40||!r.StudentNumber.All(c=>char.IsLetterOrDigit(c)||c is '-' or '/'))errors["studentNumber"]=["Use a student number up to 40 letters, digits, hyphens or slashes."];
        if(!EmailValid(r.Email))errors["email"]=["Enter a valid student email."];
        if(!EmailValid(r.GuardianEmail))errors["guardianEmail"]=["Enter a valid guardian email."];
        if(string.IsNullOrWhiteSpace(r.GuardianName)||r.GuardianName.Length>120)errors["guardianName"]=["Enter a guardian name up to 120 characters."];
        if(!Statuses.Contains(r.Status)||r.Status=="Archived")errors["status"]=["Select a supported status; archive through the approval action."];
        if(r.Semester is <1 or >12||r.BatchYear is <2000 or >2100||string.IsNullOrWhiteSpace(r.Section)||r.Section.Length>12)errors["enrollment"]=["Select a valid batch, semester and section."];
        return errors;
    }
    public static void MapStudents(this RouteGroupBuilder group,WebApplication app)
    {
        var students=group.MapGroup("/students").RequireAuthorization("Manage");
        students.MapGet("",async(Guid institutionId,string? search,string? status,Guid? programmeId,int? page,int? pageSize,ErpDbContext db)=>
        {
            var p=Math.Clamp(page??1,1,10000);var size=Math.Clamp(pageSize??20,1,100);var query=db.Students.Where(x=>x.InstitutionId==institutionId);
            if(!string.IsNullOrWhiteSpace(search)){var term=search.Trim();if(term.Length>100)return Results.Problem("Search is too long.",statusCode:400);query=query.Where(x=>EF.Functions.ILike(x.FullName,"%"+term+"%")||EF.Functions.ILike(x.StudentNumber,"%"+term+"%"));}
            if(!string.IsNullOrWhiteSpace(status))query=query.Where(x=>x.Status==status);
            if(programmeId!=null)query=query.Where(x=>db.Enrollments.Any(e=>e.InstitutionId==institutionId&&e.StudentId==x.Id&&e.ProgrammeId==programmeId));
            var total=await query.CountAsync();
            var items=await (from s in query join e in db.Enrollments on new{s.InstitutionId,StudentId=s.Id} equals new{e.InstitutionId,e.StudentId} join programme in db.Programmes on new{e.InstitutionId,Id=e.ProgrammeId} equals new{programme.InstitutionId,programme.Id} orderby s.StudentNumber select new{s.Id,s.StudentNumber,s.FullName,s.Email,s.Status,s.IsDemo,s.Version,programme=programme.Name,e.BatchYear,e.Semester,e.Section}).Skip((p-1)*size).Take(size).ToListAsync();
            return Results.Ok(new{items,total,page=p,pageSize=size});
        });
        students.MapGet("/{studentId:guid}",async(Guid institutionId,Guid studentId,ErpDbContext db)=>
        {
            var student=await db.Students.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==studentId);
            if(student==null)return Results.NotFound();
            return Results.Ok(new{student,guardian=await db.Guardians.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==student.GuardianId),enrollment=await db.Enrollments.SingleAsync(x=>x.InstitutionId==institutionId&&x.StudentId==studentId)});
        });
        students.MapPost("",async(Guid institutionId,StudentRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            var errors=Validate(r);if(errors.Count>0)return Results.ValidationProblem(errors);
            if(!await db.Programmes.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.ProgrammeId)||!await db.AcademicSessions.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.AcademicSessionId))return Results.Problem("Programme and session must belong to this institution.",statusCode:400);
            if(await db.Students.AnyAsync(x=>x.InstitutionId==institutionId&&x.StudentNumber==r.StudentNumber))return Results.Problem("Student number already exists.",statusCode:409);
            var demo=app.Environment.IsDevelopment();var guardian=new Guardian{InstitutionId=institutionId,FullName=r.GuardianName.Trim(),Email=r.GuardianEmail,IsDemo=demo};var student=new Student{InstitutionId=institutionId,StudentNumber=r.StudentNumber,FullName=r.FullName.Trim(),Email=r.Email,Status=r.Status,GuardianId=guardian.Id,IsDemo=demo};
            db.Guardians.Add(guardian);db.Students.Add(student);db.Enrollments.Add(new Enrollment{InstitutionId=institutionId,StudentId=student.Id,ProgrammeId=r.ProgrammeId,AcademicSessionId=r.AcademicSessionId,BatchYear=r.BatchYear,Semester=r.Semester,Section=r.Section.Trim().ToUpperInvariant(),IsDemo=demo});
            db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="Student.Created",Resource=student.Id.ToString(),Details="Student profile, guardian and enrollment created."});
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Student number already exists.",statusCode:409);}
            return Results.Created($"/api/v1/institutions/{institutionId}/students/{student.Id}",student);
        });
        students.MapPut("/{studentId:guid}",async(Guid institutionId,Guid studentId,StudentRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            var errors=Validate(r);if(errors.Count>0)return Results.ValidationProblem(errors);
            var student=await db.Students.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==studentId);if(student==null)return Results.NotFound();
            if(student.Version!=r.Version)return Results.Problem("This record changed. Reload before saving.",statusCode:409);
            if(student.Status=="Archived")return Results.Problem("Archived profiles cannot be edited.",statusCode:409);
            if(!await db.Programmes.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.ProgrammeId)||!await db.AcademicSessions.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.AcademicSessionId))return Results.Problem("Programme and session must belong to this institution.",statusCode:400);
            var guardian=await db.Guardians.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==student.GuardianId);var enrollment=await db.Enrollments.SingleAsync(x=>x.InstitutionId==institutionId&&x.StudentId==studentId);
            student.FullName=r.FullName.Trim();student.Email=r.Email;student.StudentNumber=r.StudentNumber;student.Status=r.Status;student.Version++;
            guardian.FullName=r.GuardianName.Trim();guardian.Email=r.GuardianEmail;enrollment.ProgrammeId=r.ProgrammeId;enrollment.AcademicSessionId=r.AcademicSessionId;enrollment.BatchYear=r.BatchYear;enrollment.Semester=r.Semester;enrollment.Section=r.Section.Trim().ToUpperInvariant();
            db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="Student.Updated",Resource=studentId.ToString(),Details=$"Profile revised from version {r.Version} to {student.Version}."});
            try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("This record changed. Reload before saving.",statusCode:409);}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Student number already exists.",statusCode:409);}
            return Results.Ok(student);
        });
        students.MapPost("/{studentId:guid}/archive",async(Guid institutionId,Guid studentId,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Length>500)return Results.Problem("An archive reason up to 500 characters is required.",statusCode:400);
            var student=await db.Students.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==studentId);if(student==null)return Results.NotFound();if(student.Version!=r.Version||student.Status=="Archived")return Results.Problem("Record changed or already archived.",statusCode:409);
            student.Status="Archived";student.Version++;db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="Student.Archived",Resource=studentId.ToString(),Details=r.Reason});try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("Record changed.",statusCode:409);}return Results.NoContent();
        });
        students.MapGet("/export",async(Guid institutionId,ErpDbContext db)=>
        {
            var rows=await db.Students.Where(x=>x.InstitutionId==institutionId).OrderBy(x=>x.StudentNumber).Select(x=>new{x.StudentNumber,x.FullName,x.Email,x.Status,x.IsDemo}).ToListAsync();
            static string Cell(string value)=>"\""+((value.Length>0&&"=+-@".Contains(value[0]))?"'":"")+value.Replace("\"","\"\"")+"\"";
            var csv="Student number,Name,Email,Status,Data mode\r\n"+string.Join("\r\n",rows.Select(x=>string.Join(",",new[]{x.StudentNumber,x.FullName,x.Email,x.Status,x.IsDemo?"DEMO":"LIVE"}.Select(Cell))));
            return Results.File(Encoding.UTF8.GetBytes(csv),"text/csv; charset=utf-8","students.csv");
        });
        group.MapGet("/student-summary",async(Guid institutionId,ErpDbContext db)=>Results.Ok(new{total=await db.Students.CountAsync(x=>x.InstitutionId==institutionId&&x.Status!="Archived"),active=await db.Students.CountAsync(x=>x.InstitutionId==institutionId&&x.Status=="Active"),newAdmissions=await db.Students.CountAsync(x=>x.InstitutionId==institutionId&&x.Status=="NewlyAdmitted"),faculty=await db.Faculty.CountAsync(x=>x.InstitutionId==institutionId&&!x.IsArchived),byStatus=await db.Students.Where(x=>x.InstitutionId==institutionId&&x.Status!="Archived").GroupBy(x=>x.Status).Select(x=>new{name=x.Key,value=x.Count()}).ToListAsync(),recent=await db.Students.Where(x=>x.InstitutionId==institutionId&&x.Status!="Archived").OrderByDescending(x=>x.CreatedAt).Take(5).Select(x=>new{x.Id,x.FullName,x.StudentNumber,x.Status}).ToListAsync()})).RequireAuthorization("Manage");
    }
}
