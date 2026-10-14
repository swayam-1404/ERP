using Microsoft.EntityFrameworkCore;
using Npgsql;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;
public record ClassroomRequest(string Name,int Capacity);
public record SubjectRequest(string Code,string Name,Guid ProgrammeId,Guid AcademicSessionId,int Semester);
public record ClassRequest(Guid SubjectId,Guid FacultyId,Guid ClassroomId,string Section,DateTimeOffset StartsAt,DateTimeOffset EndsAt);
public record AttendanceItem(Guid StudentId,string Status);
public record AttendanceRequest(int Version,AttendanceItem[] Items);
public record CorrectionRequest(Guid AttendanceRecordId,string NewStatus,string Reason);
public record ReviewRequest(bool Approve,string Note);
public static class AcademicEndpoints
{
    private static readonly string[] Statuses=["Present","Absent","Excused"];
    private static bool Manager(HttpContext ctx)=>ctx.User.IsInRole("CentralAdministrator")||ctx.User.IsInRole("InstitutionAdministrator");
    private static Task<bool> Allowed(HttpContext ctx,ErpDbContext db,ClassSession session)=>Manager(ctx)?Task.FromResult(true):db.Faculty.AnyAsync(x=>x.InstitutionId==session.InstitutionId&&x.Id==session.FacultyId&&x.UserId==ctx.Actor()&&!x.IsArchived);
    private static void Audit(ErpDbContext db,HttpContext ctx,Guid institution,string action,Guid resource,string details)=>db.AuditLogs.Add(new AuditLog{InstitutionId=institution,ActorId=ctx.Actor(),Action=action,Resource=resource.ToString(),Details=details});
    private static async Task<ClassSession?> Lock(ErpDbContext db,Guid institution,Guid id)=> (await db.ClassSessions.FromSqlInterpolated($"SELECT * FROM \"ClassSessions\" WHERE \"InstitutionId\"={institution} AND \"Id\"={id} FOR UPDATE").ToListAsync()).SingleOrDefault();
    public static void MapAcademics(this RouteGroupBuilder group,WebApplication app)
    {
        var academic=group.MapGroup("/academic").RequireAuthorization("Teaching");
        academic.MapGet("/configuration",async(Guid institutionId,HttpContext ctx,ErpDbContext db)=>Results.Ok(new{
            subjects=await db.Subjects.Where(x=>x.InstitutionId==institutionId).OrderBy(x=>x.Name).ToListAsync(),
            classrooms=await db.Classrooms.Where(x=>x.InstitutionId==institutionId).OrderBy(x=>x.Name).ToListAsync(),
            faculty=await db.Faculty.Where(x=>x.InstitutionId==institutionId&&!x.IsArchived&&(Manager(ctx)||x.UserId==ctx.Actor())).OrderBy(x=>x.FullName).Select(x=>new{x.Id,x.FullName}).ToListAsync()
        }));
        academic.MapPost("/classrooms",async(Guid institutionId,ClassroomRequest r,HttpContext ctx,ErpDbContext db)=>{
            if(string.IsNullOrWhiteSpace(r.Name)||r.Name.Trim().Length>120||r.Capacity is <1 or >1000)return Results.Problem("Enter a room name and capacity from 1 to 1000.",statusCode:400);
            var room=new Classroom{InstitutionId=institutionId,Name=r.Name.Trim(),Capacity=r.Capacity,IsDemo=app.Environment.IsDevelopment()};db.Classrooms.Add(room);Audit(db,ctx,institutionId,"Classroom.Created",room.Id,room.Name);
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Room name already exists.",statusCode:409);}return Results.Created($"/api/v1/institutions/{institutionId}/academic/configuration",room);
        }).RequireAuthorization("Manage");
        academic.MapPost("/subjects",async(Guid institutionId,SubjectRequest r,HttpContext ctx,ErpDbContext db)=>{
            if(string.IsNullOrWhiteSpace(r.Code)||r.Code.Trim().Length>40||string.IsNullOrWhiteSpace(r.Name)||r.Name.Length>160||r.Semester is <1 or >12)return Results.Problem("Enter a subject code, name and semester from 1 to 12.",statusCode:400);
            if(!await db.Programmes.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.ProgrammeId)||!await db.AcademicSessions.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.AcademicSessionId))return Results.Problem("Programme and session must belong to this institution.",statusCode:400);
            var subject=new Subject{InstitutionId=institutionId,Code=r.Code.Trim().ToUpperInvariant(),Name=r.Name.Trim(),ProgrammeId=r.ProgrammeId,AcademicSessionId=r.AcademicSessionId,Semester=r.Semester,IsDemo=app.Environment.IsDevelopment()};db.Subjects.Add(subject);Audit(db,ctx,institutionId,"Subject.Created",subject.Id,subject.Code);
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Subject code already exists in this programme/session.",statusCode:409);}return Results.Created($"/api/v1/institutions/{institutionId}/academic/configuration",subject);
        }).RequireAuthorization("Manage");
        academic.MapGet("/classes",async(Guid institutionId,DateTimeOffset? from,DateTimeOffset? to,int? page,HttpContext ctx,ErpDbContext db)=>{
            var start=(from??DateTimeOffset.UtcNow.AddDays(-30)).UtcDateTime;var end=(to??DateTimeOffset.UtcNow.AddDays(30)).UtcDateTime;if(end<=start||end-start>TimeSpan.FromDays(120))return Results.Problem("Select a date range up to 120 days.",statusCode:400);
            var query=db.ClassSessions.Where(x=>x.InstitutionId==institutionId&&x.StartsAt>=start&&x.StartsAt<end&&(Manager(ctx)||db.Faculty.Any(f=>f.InstitutionId==institutionId&&f.Id==x.FacultyId&&f.UserId==ctx.Actor()&&!f.IsArchived)));var p=Math.Clamp(page??1,1,10000);
            var items=await(from c in query join s in db.Subjects on c.SubjectId equals s.Id join f in db.Faculty on c.FacultyId equals f.Id join room in db.Classrooms on c.ClassroomId equals room.Id orderby c.StartsAt descending select new{c.Id,c.StartsAt,c.EndsAt,c.Section,c.Semester,c.Version,c.IsCancelled,c.AttendanceSubmitted,c.IsDemo,subject=s.Name,subjectCode=s.Code,faculty=f.FullName,classroom=room.Name,rosterCount=db.ClassRosters.Count(r=>r.InstitutionId==institutionId&&r.ClassSessionId==c.Id)}).Skip((p-1)*25).Take(25).ToListAsync();return Results.Ok(new{items,total=await query.CountAsync(),page=p,pageSize=25});
        });
        academic.MapPost("/classes",async(Guid institutionId,ClassRequest r,HttpContext ctx,ErpDbContext db)=>{
            if(string.IsNullOrWhiteSpace(r.Section)||r.Section.Trim().Length>12||r.EndsAt<=r.StartsAt||r.EndsAt-r.StartsAt>TimeSpan.FromHours(4))return Results.Problem("Provide a section and a class period between 1 minute and 4 hours.",statusCode:400);
            if(r.EndsAt-r.StartsAt<TimeSpan.FromMinutes(1))return Results.Problem("Class duration must be at least one minute.",statusCode:400);
            var subject=await db.Subjects.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==r.SubjectId);var room=await db.Classrooms.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==r.ClassroomId);
            if(subject==null||room==null||!await db.Faculty.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.FacultyId&&!x.IsArchived))return Results.Problem("Subject, room and active faculty must belong to this institution.",statusCode:400);
            var section=r.Section.Trim().ToUpperInvariant();var roster=await(from e in db.Enrollments join student in db.Students on new{e.InstitutionId,Id=e.StudentId} equals new{student.InstitutionId,student.Id} where e.InstitutionId==institutionId&&e.ProgrammeId==subject.ProgrammeId&&e.AcademicSessionId==subject.AcademicSessionId&&e.Semester==subject.Semester&&e.Section==section&&(student.Status=="Active"||student.Status=="NewlyAdmitted") select new{e.Id,e.StudentId}).ToListAsync();
            if(roster.Count==0)return Results.Problem("No active enrolled students match this subject semester and section.",statusCode:400);if(roster.Count>room.Capacity)return Results.Problem("Roster exceeds room capacity.",statusCode:400);
            var session=new ClassSession{InstitutionId=institutionId,SubjectId=subject.Id,ProgrammeId=subject.ProgrammeId,AcademicSessionId=subject.AcademicSessionId,Semester=subject.Semester,FacultyId=r.FacultyId,ClassroomId=r.ClassroomId,Section=section,StartsAt=r.StartsAt.UtcDateTime,EndsAt=r.EndsAt.UtcDateTime,IsDemo=app.Environment.IsDevelopment()};db.ClassSessions.Add(session);
            foreach(var member in roster)db.ClassRosters.Add(new ClassRoster{InstitutionId=institutionId,ClassSessionId=session.Id,EnrollmentId=member.Id,StudentId=member.StudentId});Audit(db,ctx,institutionId,"Class.Scheduled",session.Id,$"{subject.Code}; roster {roster.Count}; {session.StartsAt:O}–{session.EndsAt:O}.");
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.ExclusionViolation}){return Results.Problem("This period conflicts with a room, faculty or class-group booking.",statusCode:409);}return Results.Created($"/api/v1/institutions/{institutionId}/academic/classes/{session.Id}/attendance",session);
        }).RequireAuthorization("Manage");
        academic.MapPost("/classes/{classId:guid}/cancel",async(Guid institutionId,Guid classId,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>{
            if(string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Length>500)return Results.Problem("Provide a cancellation reason up to 500 characters.",statusCode:400);await using var tx=await db.Database.BeginTransactionAsync();var c=await Lock(db,institutionId,classId);if(c==null)return Results.NotFound();if(c.Version!=r.Version||c.IsCancelled||c.AttendanceSubmitted)return Results.Problem("Class changed, cancelled or already has attendance.",statusCode:409);c.IsCancelled=true;c.Version++;Audit(db,ctx,institutionId,"Class.Cancelled",c.Id,r.Reason);await db.SaveChangesAsync();await tx.CommitAsync();return Results.NoContent();
        }).RequireAuthorization("Manage");
        academic.MapGet("/classes/{classId:guid}/attendance",async(Guid institutionId,Guid classId,HttpContext ctx,ErpDbContext db)=>{
            var c=await db.ClassSessions.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==classId);if(c==null)return Results.NotFound();if(!await Allowed(ctx,db,c))return Results.Problem("Class is not assigned to you.",statusCode:403);
            var roster=await(from r in db.ClassRosters join s in db.Students on new{r.InstitutionId,Id=r.StudentId} equals new{s.InstitutionId,s.Id} where r.InstitutionId==institutionId&&r.ClassSessionId==classId orderby s.StudentNumber select new{s.Id,s.FullName,s.StudentNumber}).ToListAsync();return Results.Ok(new{session=c,roster,records=await db.AttendanceRecords.Where(x=>x.InstitutionId==institutionId&&x.ClassSessionId==classId).ToListAsync()});
        });
        academic.MapPost("/classes/{classId:guid}/attendance",async(Guid institutionId,Guid classId,AttendanceRequest r,HttpContext ctx,ErpDbContext db)=>{
            if(r.Items==null||r.Items.Length>1000||r.Items.Any(x=>x==null||!Statuses.Contains(x.Status))||r.Items.Select(x=>x.StudentId).Distinct().Count()!=r.Items.Length)return Results.Problem("Provide distinct students with Present, Absent or Excused status.",statusCode:400);
            await using var tx=await db.Database.BeginTransactionAsync();var c=await Lock(db,institutionId,classId);if(c==null)return Results.NotFound();if(!await Allowed(ctx,db,c))return Results.Problem("Class is not assigned to you.",statusCode:403);
            if(c.AttendanceSubmitted||c.IsCancelled||c.Version!=r.Version)return Results.Problem("Attendance already submitted, or class changed/cancelled.",statusCode:409);if(c.StartsAt>DateTime.UtcNow)return Results.Problem("Attendance cannot be recorded before the class starts.",statusCode:400);
            var roster=await db.ClassRosters.Where(x=>x.InstitutionId==institutionId&&x.ClassSessionId==classId).Select(x=>x.StudentId).ToListAsync();if(!roster.ToHashSet().SetEquals(r.Items.Select(x=>x.StudentId)))return Results.Problem("Submit every roster member once; students outside the roster are rejected.",statusCode:400);
            foreach(var item in r.Items)db.AttendanceRecords.Add(new AttendanceRecord{InstitutionId=institutionId,ClassSessionId=classId,StudentId=item.StudentId,Status=item.Status,RecordedBy=ctx.Actor()});c.AttendanceSubmitted=true;c.Version++;Audit(db,ctx,institutionId,"Attendance.Submitted",classId,$"Present {r.Items.Count(x=>x.Status=="Present")}, absent {r.Items.Count(x=>x.Status=="Absent")}, excused {r.Items.Count(x=>x.Status=="Excused")}.");await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{recorded=r.Items.Length,version=c.Version});
        });
        academic.MapPost("/corrections",async(Guid institutionId,CorrectionRequest r,HttpContext ctx,ErpDbContext db)=>{
            if(!Statuses.Contains(r.NewStatus)||string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Length is <5 or >500)return Results.Problem("Provide a supported status and reason from 5 to 500 characters.",statusCode:400);
            var record=await db.AttendanceRecords.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==r.AttendanceRecordId);if(record==null)return Results.NotFound();var c=await db.ClassSessions.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==record.ClassSessionId);if(!await Allowed(ctx,db,c))return Results.Problem("Class is not assigned to you.",statusCode:403);if(record.Status==r.NewStatus)return Results.Problem("The new status matches the existing record.",statusCode:400);
            var correction=new AttendanceCorrection{InstitutionId=institutionId,AttendanceRecordId=record.Id,RequestedBy=ctx.Actor(),OriginalStatus=record.Status,NewStatus=r.NewStatus,Reason=r.Reason.Trim()};db.AttendanceCorrections.Add(correction);Audit(db,ctx,institutionId,"Attendance.CorrectionRequested",correction.Id,$"{record.Status} → {r.NewStatus}; {r.Reason}");await db.SaveChangesAsync();return Results.Created($"/api/v1/institutions/{institutionId}/academic/corrections",correction);
        });
        academic.MapGet("/corrections",async(Guid institutionId,HttpContext ctx,ErpDbContext db)=>Results.Ok(await db.AttendanceCorrections.Where(x=>x.InstitutionId==institutionId&&(Manager(ctx)||x.RequestedBy==ctx.Actor())).OrderByDescending(x=>x.RequestedAt).Take(100).ToListAsync()));
        academic.MapPost("/corrections/{correctionId:guid}/review",async(Guid institutionId,Guid correctionId,ReviewRequest r,HttpContext ctx,ErpDbContext db)=>{
            if(string.IsNullOrWhiteSpace(r.Note)||r.Note.Length>500)return Results.Problem("Provide a review note up to 500 characters.",statusCode:400);await using var tx=await db.Database.BeginTransactionAsync();var correction=await db.AttendanceCorrections.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==correctionId);if(correction==null)return Results.NotFound();if(correction.RequestedBy==ctx.Actor())return Results.Problem("A different administrator must review this request.",statusCode:403);
            var record=await db.AttendanceRecords.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==correction.AttendanceRecordId);await Lock(db,institutionId,record.ClassSessionId);await db.Entry(correction).ReloadAsync();await db.Entry(record).ReloadAsync();if(correction.Status!="Pending")return Results.Problem("Correction already reviewed.",statusCode:409);if(r.Approve&&record.Status!=correction.OriginalStatus)return Results.Problem("Attendance changed since this request. Submit a new correction.",statusCode:409);
            if(r.Approve)record.Status=correction.NewStatus;correction.Status=r.Approve?"Approved":"Rejected";correction.ReviewedBy=ctx.Actor();correction.ReviewedAt=DateTime.UtcNow;correction.ReviewNote=r.Note;Audit(db,ctx,institutionId,"Attendance.Correction"+correction.Status,correction.Id,$"{correction.OriginalStatus} → {correction.NewStatus}; {r.Note}");await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(correction);
        }).RequireAuthorization("Manage");
        academic.MapGet("/report",async(Guid institutionId,Guid? studentId,HttpContext ctx,ErpDbContext db)=>{
            var records=from a in db.AttendanceRecords join c in db.ClassSessions on a.ClassSessionId equals c.Id join subject in db.Subjects on c.SubjectId equals subject.Id where a.InstitutionId==institutionId&&!c.IsCancelled&&(studentId==null||a.StudentId==studentId)&&(Manager(ctx)||db.Faculty.Any(f=>f.InstitutionId==institutionId&&f.Id==c.FacultyId&&f.UserId==ctx.Actor()&&!f.IsArchived)) select new{subject.Id,subject.Name,a.Status};
            var grouped=await records.GroupBy(x=>new{x.Id,x.Name}).Select(x=>new{subjectId=x.Key.Id,subject=x.Key.Name,present=x.Count(a=>a.Status=="Present"),absent=x.Count(a=>a.Status=="Absent"),excused=x.Count(a=>a.Status=="Excused")}).ToListAsync();return Results.Ok(grouped.Select(x=>new{x.subjectId,x.subject,x.present,x.absent,x.excused,percentage=x.present+x.absent==0?(decimal?)null:decimal.Round(100m*x.present/(x.present+x.absent),2)}));
        });
    }
}
