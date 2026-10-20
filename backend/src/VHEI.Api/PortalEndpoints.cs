using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;
public record PortalAccountRequest(string Kind,string Email,string? TemporaryPassword,string EvidenceReference);
public static class PortalEndpoints
{
    internal static IQueryable<Student> Accessible(ErpDbContext db,HttpContext ctx,Guid institution)=>db.Students.Where(s=>s.InstitutionId==institution&&s.Status!="Archived"&&db.PortalAccesses.Any(a=>a.InstitutionId==institution&&a.StudentId==s.Id&&a.UserId==ctx.Actor()&&a.IsActive&&((a.Kind=="Student"&&ctx.User.IsInRole("Student"))||(a.Kind=="Guardian"&&ctx.User.IsInRole("Guardian")))));
    public static void MapPortal(this RouteGroupBuilder group,WebApplication app)
    {
        var portal=group.MapGroup("/portal").RequireAuthorization(p=>p.RequireRole("Student","Guardian"));
        portal.MapGet("/students",async(Guid institutionId,HttpContext ctx,ErpDbContext db)=>Results.Ok(await Accessible(db,ctx,institutionId).OrderBy(x=>x.StudentNumber).Select(x=>new{x.Id,x.StudentNumber,x.FullName,x.Status,x.IsDemo}).ToListAsync()));
        portal.MapGet("/students/{studentId:guid}",async(Guid institutionId,Guid studentId,HttpContext ctx,ErpDbContext db)=>
        {
            var s=await Accessible(db,ctx,institutionId).SingleOrDefaultAsync(x=>x.Id==studentId);if(s==null)return Results.NotFound();
            var enrollment=await(from e in db.Enrollments join p in db.Programmes on new{e.InstitutionId,Id=e.ProgrammeId} equals new{p.InstitutionId,p.Id} join session in db.AcademicSessions on new{e.InstitutionId,Id=e.AcademicSessionId} equals new{session.InstitutionId,session.Id} where e.InstitutionId==institutionId&&e.StudentId==s.Id select new{e.Id,e.BatchYear,e.Semester,e.Section,programme=p.Name,session=session.Name}).ToListAsync();
            var guardian=await db.Guardians.Where(x=>x.InstitutionId==institutionId&&x.Id==s.GuardianId).Select(x=>new{x.FullName,x.Email,x.Relationship}).SingleAsync();
            var attendance=await(from a in db.AttendanceRecords join c in db.ClassSessions on new{a.InstitutionId,Id=a.ClassSessionId} equals new{c.InstitutionId,c.Id} join subject in db.Subjects on new{c.InstitutionId,Id=c.SubjectId} equals new{subject.InstitutionId,subject.Id} where a.InstitutionId==institutionId&&a.StudentId==studentId&&!c.IsCancelled group a by new{subject.Id,subject.Name} into g select new{subjectId=g.Key.Id,subject=g.Key.Name,present=g.Count(x=>x.Status=="Present"),absent=g.Count(x=>x.Status=="Absent"),excused=g.Count(x=>x.Status=="Excused")}).ToListAsync();
            return Results.Ok(new{student=new{s.Id,s.FullName,s.StudentNumber,s.Email,s.Status,s.IsDemo},guardian,enrollment,attendance});
        });
        portal.MapGet("/students/{studentId:guid}/classes",async(Guid institutionId,Guid studentId,int? page,HttpContext ctx,ErpDbContext db)=>
        {
            if(!await Accessible(db,ctx,institutionId).AnyAsync(x=>x.Id==studentId))return Results.NotFound();
            var start=DateTime.UtcNow.AddDays(-30);var end=DateTime.UtcNow.AddDays(90);var p=Math.Clamp(page??1,1,10000);
            var query=from c in db.ClassSessions join s in db.Subjects on new{c.InstitutionId,Id=c.SubjectId} equals new{s.InstitutionId,s.Id} join room in db.Classrooms on new{c.InstitutionId,Id=c.ClassroomId} equals new{room.InstitutionId,room.Id} where c.InstitutionId==institutionId&&c.StartsAt>=start&&c.StartsAt<end&&db.ClassRosters.Any(r=>r.InstitutionId==institutionId&&r.ClassSessionId==c.Id&&r.StudentId==studentId) select new{c.Id,c.StartsAt,c.EndsAt,c.IsCancelled,c.AttendanceSubmitted,subject=s.Name,classroom=room.Name,status=db.AttendanceRecords.Where(a=>a.InstitutionId==institutionId&&a.ClassSessionId==c.Id&&a.StudentId==studentId).Select(a=>a.Status).FirstOrDefault()};
            return Results.Ok(new{items=await query.OrderByDescending(x=>x.StartsAt).ThenBy(x=>x.Id).Skip((p-1)*25).Take(25).ToListAsync(),total=await query.CountAsync(),page=p});
        });
        portal.MapGet("/students/{studentId:guid}/fees",async(Guid institutionId,Guid studentId,HttpContext ctx,ErpDbContext db)=>
        {
            if(!await Accessible(db,ctx,institutionId).AnyAsync(x=>x.Id==studentId))return Results.NotFound();
            var charges=await(from c in db.AdmissionCharges join a in db.AdmissionApplications on new{c.InstitutionId,Id=c.ApplicationId} equals new{a.InstitutionId,a.Id} where c.InstitutionId==institutionId&&a.StudentId==studentId select new{c.Id,c.Amount,c.Currency,c.IsVoid,a.ApplicationNumber}).ToListAsync();
            var ids=charges.Select(x=>x.Id).ToList();var payments=await db.AdmissionPayments.Where(x=>x.InstitutionId==institutionId&&ids.Contains(x.ChargeId)).Select(x=>new{x.Id,x.ChargeId,x.Amount,x.PaidOn,x.Status,x.AcknowledgementNumber}).ToListAsync();return Results.Ok(new{charges,payments});
        });
        portal.MapGet("/students/{studentId:guid}/payments/{paymentId:guid}/acknowledgement",async(Guid institutionId,Guid studentId,Guid paymentId,HttpContext ctx,ErpDbContext db)=>
        {
            if(!await Accessible(db,ctx,institutionId).AnyAsync(x=>x.Id==studentId))return Results.NotFound();
            var p=await(from payment in db.AdmissionPayments join charge in db.AdmissionCharges on new{payment.InstitutionId,Id=payment.ChargeId} equals new{charge.InstitutionId,charge.Id} join a in db.AdmissionApplications on new{charge.InstitutionId,Id=charge.ApplicationId} equals new{a.InstitutionId,a.Id} where payment.InstitutionId==institutionId&&payment.Id==paymentId&&payment.Status=="Confirmed"&&a.StudentId==studentId select new{payment.Amount,payment.PaidOn,payment.AcknowledgementNumber,a.ApplicationNumber,a.IsDemo}).SingleOrDefaultAsync();if(p==null)return Results.NotFound();
            var text=$"{(p.IsDemo?"DEMO — SYNTHETIC RECORD":"OFFLINE PAYMENT ACKNOWLEDGEMENT")}\n{p.AcknowledgementNumber}\nApplication: {p.ApplicationNumber}\nINR {p.Amount:F2}\nPayment date: {p.PaidOn:yyyy-MM-dd}\nERP acknowledgement of independently checked evidence; not a bank settlement confirmation or tax invoice.\n";return Results.File(System.Text.Encoding.UTF8.GetBytes(text),"text/plain; charset=utf-8",p.AcknowledgementNumber+".txt");
        });
        var admin=group.MapGroup("/portal-access").RequireAuthorization("Manage");
        admin.MapGet("",async(Guid institutionId,Guid? studentId,int? page,ErpDbContext db)=>
        {
            var query=from access in db.PortalAccesses join user in db.Users on access.UserId equals user.Id join student in db.Students on new{access.InstitutionId,Id=access.StudentId} equals new{student.InstitutionId,student.Id} where access.InstitutionId==institutionId&&(studentId==null||access.StudentId==studentId) select new{access.Id,access.StudentId,access.Kind,access.IsActive,access.Version,access.EvidenceReference,access.RevocationReason,user.Email,user.MustChangePassword,student=student.FullName,student.StudentNumber};var p=Math.Clamp(page??1,1,10000);return Results.Ok(new{items=await query.OrderBy(x=>x.StudentNumber).ThenBy(x=>x.Id).Skip((p-1)*25).Take(25).ToListAsync(),total=await query.CountAsync(),page=p});
        });
        admin.MapPost("/students/{studentId:guid}",async(Guid institutionId,Guid studentId,PortalAccountRequest r,HttpContext ctx,ErpDbContext db,UserManager<ErpUser> users,RoleManager<IdentityRole<Guid>> roles)=>
        {
            if(r.Kind is not("Student" or "Guardian")||!StudentEndpoints.EmailValid(r.Email)||string.IsNullOrWhiteSpace(r.EvidenceReference)||r.EvidenceReference.Trim().Length<3||r.EvidenceReference.Length>500)return Results.Problem("Choose Student or Guardian, a valid matching email, and a verified identity/relationship reference.",statusCode:400);
            await using var tx=await db.Database.BeginTransactionAsync();
            var student=(await db.Students.FromSqlInterpolated($"SELECT * FROM \"Students\" WHERE \"InstitutionId\"={institutionId} AND \"Id\"={studentId} FOR UPDATE").ToListAsync()).SingleOrDefault();if(student==null)return Results.NotFound();if(student.Status=="Archived")return Results.Problem("Archived students cannot receive portal access.",statusCode:409);
            var guardian=await db.Guardians.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==student.GuardianId);var expected=r.Kind=="Student"?student.Email:guardian.Email;
            if(!string.Equals(expected.Trim(),r.Email.Trim(),StringComparison.OrdinalIgnoreCase))return Results.Problem("Email must match the verified student or guardian record. Update that record through its authorised workflow first.",statusCode:400);
            var user=await users.FindByEmailAsync(r.Email.Trim());
            if(user==null)
            {
                if(string.IsNullOrEmpty(r.TemporaryPassword)||r.TemporaryPassword.Length is <12 or >128)return Results.Problem("New accounts need a strong temporary password of 12–128 characters.",statusCode:400);
                user=new ErpUser{UserName=r.Email.Trim(),Email=r.Email.Trim(),DisplayName=r.Kind=="Student"?student.FullName:guardian.FullName,MustChangePassword=true};
                try{var created=await users.CreateAsync(user,r.TemporaryPassword);if(!created.Succeeded)return Results.Problem("Account creation rejected. Check the email and password policy.",statusCode:400);}
                catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Account was created concurrently. Reload before granting access.",statusCode:409);}
                if(!await roles.RoleExistsAsync(r.Kind))
                {
                    try{var role=await roles.CreateAsync(new IdentityRole<Guid>(r.Kind));if(!role.Succeeded)return Results.Problem("Role setup changed; no access was granted. Retry after reloading.",statusCode:409);}
                    catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Role setup changed; retry after reloading.",statusCode:409);}
                }
                var assigned=await users.AddToRoleAsync(user,r.Kind);if(!assigned.Succeeded)return Results.Problem("Role assignment failed; no access was granted.",statusCode:409);
            }
            else if((await users.GetRolesAsync(user)) is var assignedRoles&&(!assignedRoles.Contains(r.Kind)||assignedRoles.Any(x=>x!=r.Kind)))return Results.Problem("Existing account has incompatible roles. No roles or passwords were changed.",statusCode:409);
            if(await db.PortalAccesses.AnyAsync(x=>x.InstitutionId==institutionId&&x.UserId==user.Id&&x.StudentId==studentId&&x.Kind==r.Kind))return Results.Problem("An access record already exists. Revoked records require a separately reviewed regrant workflow.",statusCode:409);
            if(!await db.InstitutionAssignments.AnyAsync(x=>x.InstitutionId==institutionId&&x.UserId==user.Id))db.InstitutionAssignments.Add(new InstitutionAssignment{InstitutionId=institutionId,UserId=user.Id});
            var access=new PortalAccess{InstitutionId=institutionId,UserId=user.Id,StudentId=studentId,Kind=r.Kind,EvidenceReference=r.EvidenceReference.Trim(),GrantedBy=ctx.Actor()};db.PortalAccesses.Add(access);db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="PortalAccess.Granted",Resource=access.Id.ToString(),Details=$"{r.Kind} access to student {studentId}; verified evidence reference retained."});
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Account or access already exists. No partial grant was saved.",statusCode:409);}await tx.CommitAsync();return Results.Created($"/api/v1/institutions/{institutionId}/portal-access",new{access.Id,userId=user.Id,user.Email,user.MustChangePassword});
        });
        admin.MapPost("/{id:guid}/revoke",async(Guid institutionId,Guid id,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Trim().Length<3||r.Reason.Length>500)return Results.Problem("Provide a revocation reason of 3–500 characters.",statusCode:400);var access=await db.PortalAccesses.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(access==null)return Results.NotFound();if(!access.IsActive||access.Version!=r.Version)return Results.Problem("Access changed or already revoked.",statusCode:409);access.IsActive=false;access.Version++;access.RevocationReason=r.Reason.Trim();db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="PortalAccess.Revoked",Resource=id.ToString(),Details=r.Reason.Trim()});try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("Access changed. Reload before revoking.",statusCode:409);}return Results.NoContent();
        });
    }
}
