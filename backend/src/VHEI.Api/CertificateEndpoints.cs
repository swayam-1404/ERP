using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
using VHEI.Infrastructure;

namespace VHEI.Api;
public record GraduationPolicyRequest(string Name, Guid ProgrammeId, Guid AcademicSessionId, Guid[] RequiredSubjectIds, decimal MinimumCredits, string SourceReference);
public record CertificateRequest(string Kind, Guid? GraduationPolicyId, string ClearanceReference);
public record CertificateIssueRequest(int Version, string Reason, string AuthorityReference);
public record GraduationEvaluation(bool Eligible, decimal PassedCredits, int RequiredSubjects, int PassedSubjects, string[] Blockers);

public static class CertificateEndpoints
{
    private static bool Text(string? text, int min=3, int max=500) => text!=null && text.Trim().Length>=min && text.Length<=max;
    private static void Audit(ErpDbContext db,HttpContext ctx,Guid institution,string action,Guid id,string note) => db.AuditLogs.Add(new AuditLog{InstitutionId=institution,ActorId=ctx.Actor(),Action="Certificate."+action,Resource=id.ToString(),Details=note});

    public static void MapCertificates(this RouteGroupBuilder group, WebApplication app)
    {
        var management=group.MapGroup("/certificates").RequireAuthorization("Manage");
        management.MapGet("/graduation-policies",async(Guid institutionId,ErpDbContext db)=>Results.Ok(await db.GraduationPolicies.Where(x=>x.InstitutionId==institutionId).OrderBy(x=>x.Name).Take(100).ToListAsync()));
        management.MapPost("/graduation-policies",async(Guid institutionId,GraduationPolicyRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Name,3,120)||!Text(r.SourceReference)||r.MinimumCredits is <=0 or >1000||decimal.Round(r.MinimumCredits,2)!=r.MinimumCredits||r.RequiredSubjectIds==null||r.RequiredSubjectIds.Length is <1 or >100||r.RequiredSubjectIds.Distinct().Count()!=r.RequiredSubjectIds.Length)
                return Results.Problem("Provide a curriculum reference, 1–100 distinct required subjects and a minimum credit total.",statusCode:400);
            if(!await db.Programmes.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.ProgrammeId)||!await db.AcademicSessions.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.AcademicSessionId)||await db.Subjects.CountAsync(x=>x.InstitutionId==institutionId&&x.ProgrammeId==r.ProgrammeId&&x.AcademicSessionId==r.AcademicSessionId&&r.RequiredSubjectIds.Contains(x.Id))!=r.RequiredSubjectIds.Length)
                return Results.Problem("Every required subject must belong to the selected programme and session in this institution.",statusCode:400);
            var policy=new GraduationPolicy{InstitutionId=institutionId,Name=r.Name.Trim(),ProgrammeId=r.ProgrammeId,AcademicSessionId=r.AcademicSessionId,RequiredSubjectsJson=JsonSerializer.Serialize(r.RequiredSubjectIds.Order()),MinimumCredits=r.MinimumCredits,SourceReference=r.SourceReference.Trim(),CreatedBy=ctx.Actor()};
            db.GraduationPolicies.Add(policy);Audit(db,ctx,institutionId,"GraduationPolicyCreated",policy.Id,"Draft explicit curriculum; approval required.");await db.SaveChangesAsync();return Results.Created($"/api/v1/institutions/{institutionId}/certificates/graduation-policies",policy);
        });
        management.MapPost("/graduation-policies/{id:guid}/approve",async(Guid institutionId,Guid id,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Reason))return Results.Problem("Provide the curriculum approval reference.",statusCode:400);
            var policy=await db.GraduationPolicies.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(policy==null)return Results.NotFound();
            if(policy.Status!="Draft"||policy.Version!=r.Version)return Results.Problem("Policy has changed or is already approved.",statusCode:409);
            if(policy.CreatedBy==ctx.Actor())return Results.Problem("Another administrator must approve the curriculum.",statusCode:403);
            policy.Status="Approved";policy.ApprovedBy=ctx.Actor();policy.ApprovalNote=r.Reason.Trim();policy.Version++;Audit(db,ctx,institutionId,"GraduationPolicyApproved",id,r.Reason.Trim());
            try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("Policy changed; reload.",statusCode:409);}return Results.Ok(policy);
        });
        management.MapGet("/students/{studentId:guid}/eligibility/{policyId:guid}",async(Guid institutionId,Guid studentId,Guid policyId,ErpDbContext db)=>
        {
            var policy=await db.GraduationPolicies.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==policyId);
            if(policy==null||!await db.Students.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==studentId&&x.Status!="Archived"))return Results.NotFound();
            return Results.Ok(await Evaluate(db,institutionId,studentId,policy));
        });
        management.MapGet("/students/{studentId:guid}",async(Guid institutionId,Guid studentId,ErpDbContext db)=>await List(db,institutionId,studentId));
        management.MapPost("/students/{studentId:guid}",async(Guid institutionId,Guid studentId,CertificateRequest r,HttpContext ctx,ErpDbContext db)=>await Request(db,ctx,institutionId,studentId,r,app.Environment.IsDevelopment()));
        management.MapGet("/{id:guid}/download",async(Guid institutionId,Guid id,ErpDbContext db)=>await Download(db,institutionId,id));
        management.MapPost("/{id:guid}/issue",async(Guid institutionId,Guid id,CertificateIssueRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Reason)||!Text(r.AuthorityReference))return Results.Problem("Provide approval and issuing authority references.",statusCode:400);
            if(!app.Environment.IsDevelopment()&&!app.Configuration.GetValue<bool>("Certificates:IssuanceEnabled"))return Results.Problem("Production issuance requires configured institutional authority and approved templates.",statusCode:409);
            var publicBase=app.Configuration["Certificates:PublicBaseUrl"]??(app.Environment.IsDevelopment()?"http://127.0.0.1:5173":null);
            if(!Uri.TryCreate(publicBase,UriKind.Absolute,out var baseUri)||(!app.Environment.IsDevelopment()&&baseUri.Scheme!="https")||baseUri.Scheme is not("http" or "https")||!string.IsNullOrEmpty(baseUri.UserInfo)||!string.IsNullOrEmpty(baseUri.Query)||!string.IsNullOrEmpty(baseUri.Fragment))return Results.Problem("A valid public verification URL is required.",statusCode:409);
            await using var tx=await db.Database.BeginTransactionAsync();
            var certificate=(await db.AcademicCertificates.FromSqlInterpolated($"SELECT * FROM \"AcademicCertificates\" WHERE \"InstitutionId\"={institutionId} AND \"Id\"={id} FOR UPDATE").ToListAsync()).SingleOrDefault();if(certificate==null)return Results.NotFound();
            if(certificate.Status!="Requested"||certificate.Version!=r.Version)return Results.Problem("Request changed or has already been processed.",statusCode:409);
            if(certificate.RequestedBy==ctx.Actor())return Results.Problem("Another administrator must approve and issue this document.",statusCode:403);
            var student=(await db.Students.FromSqlInterpolated($"SELECT * FROM \"Students\" WHERE \"InstitutionId\"={institutionId} AND \"Id\"={certificate.StudentId} FOR UPDATE").ToListAsync()).SingleOrDefault();if(student==null||student.Status=="Archived")return Results.Problem("Student is unavailable for issuance.",statusCode:409);
            var current=await Snapshot(db,institutionId,student);
            if(current!=certificate.SnapshotJson)return Results.Problem("Student details or published results changed. Create a new request for review.",statusCode:409);
            if(certificate.Kind=="CourseCompletion")
            {
                var policy=await db.GraduationPolicies.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==certificate.GraduationPolicyId);
                var evaluation=await Evaluate(db,institutionId,student.Id,policy);if(!evaluation.Eligible)return Results.Problem(string.Join(" ",evaluation.Blockers),statusCode:409);
            }
            certificate.Number=(certificate.IsDemo?"DEMO-":"VHEI-")+Guid.NewGuid().ToString("N").ToUpperInvariant();certificate.VerificationToken=Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            certificate.IssuedAt=DateTime.UtcNow;certificate.IssuedBy=ctx.Actor();certificate.AuthorityReference=r.AuthorityReference.Trim();certificate.ReviewNote=r.Reason.Trim();
            var verifyUrl=baseUri.GetLeftPart(UriPartial.Path).TrimEnd('/')+"/verify/"+certificate.VerificationToken;
            try{certificate.Pdf=CertificatePdf.Render(certificate,verifyUrl);}catch(NotSupportedException e){return Results.Problem(e.Message,statusCode:400);}certificate.Sha256=Convert.ToHexString(SHA256.HashData(certificate.Pdf)).ToLowerInvariant();certificate.Status="Issued";certificate.Version++;
            Audit(db,ctx,institutionId,"Issued",id,$"{certificate.Number}; SHA256 {certificate.Sha256}; {r.AuthorityReference.Trim()}");await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{certificate.Id,certificate.Status,certificate.Version,certificate.Number,certificate.Sha256});
        });
        management.MapPost("/{id:guid}/revoke",async(Guid institutionId,Guid id,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Reason))return Results.Problem("Provide a revocation reason.",statusCode:400);
            var c=await db.AcademicCertificates.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(c==null)return Results.NotFound();if(c.Status!="Issued"||c.Version!=r.Version)return Results.Problem("Document changed or is not issued.",statusCode:409);
            c.Status="Revoked";c.RevokedBy=ctx.Actor();c.RevokedAt=DateTime.UtcNow;c.ReviewNote=r.Reason.Trim();c.Version++;Audit(db,ctx,institutionId,"Revoked",id,r.Reason.Trim());try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("Document changed; reload.",statusCode:409);}return Results.Ok(new{c.Id,c.Status,c.Version});
        });
        management.MapPost("/{id:guid}/cancel",async(Guid institutionId,Guid id,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Reason))return Results.Problem("Provide a cancellation reason.",statusCode:400);
            var c=await db.AcademicCertificates.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(c==null)return Results.NotFound();if(c.Status!="Requested"||c.Version!=r.Version)return Results.Problem("Request changed or is already processed.",statusCode:409);
            c.Status="Cancelled";c.ReviewNote=r.Reason.Trim();c.Version++;Audit(db,ctx,institutionId,"Cancelled",id,r.Reason.Trim());try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("Request changed; reload.",statusCode:409);}return Results.Ok(new{c.Id,c.Status,c.Version});
        });
        var portal=group.MapGroup("/portal/students/{studentId:guid}/certificates").RequireAuthorization(p=>p.RequireRole("Student","Guardian"));
        portal.MapGet("",async(Guid institutionId,Guid studentId,HttpContext ctx,ErpDbContext db)=>await PortalEndpoints.Accessible(db,ctx,institutionId).AnyAsync(x=>x.Id==studentId)?await List(db,institutionId,studentId):Results.NotFound());
        portal.MapPost("",async(Guid institutionId,Guid studentId,HttpContext ctx,ErpDbContext db)=>await PortalEndpoints.Accessible(db,ctx,institutionId).AnyAsync(x=>x.Id==studentId)?await Request(db,ctx,institutionId,studentId,new("Transcript",null,""),app.Environment.IsDevelopment()):Results.NotFound());
        portal.MapGet("/{id:guid}/download",async(Guid institutionId,Guid studentId,Guid id,HttpContext ctx,ErpDbContext db)=>await PortalEndpoints.Accessible(db,ctx,institutionId).AnyAsync(x=>x.Id==studentId)&&await db.AcademicCertificates.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==id&&x.StudentId==studentId)?await Download(db,institutionId,id):Results.NotFound());
        app.MapGet("/api/v1/certificates/verify/{token}",async(string token,HttpContext ctx,ErpDbContext db)=>
        {
            ctx.Response.Headers.CacheControl="no-store";ctx.Response.Headers["X-Robots-Tag"]="noindex, nofollow";
            if(token.Length!=48||token.Any(c=>!char.IsAsciiHexDigit(c)))return Results.NotFound();
            var c=await db.AcademicCertificates.AsNoTracking().Where(x=>x.VerificationToken==token&&x.Status!="Requested").Select(x=>new{x.Number,x.Kind,x.Status,x.IsDemo,x.IssuedAt,x.RevokedAt,x.Sha256,x.InstitutionId}).SingleOrDefaultAsync();if(c==null)return Results.NotFound();
            var institution=await db.Institutions.Where(x=>x.Id==c.InstitutionId).Select(x=>x.Name).SingleAsync();return Results.Ok(new{c.Number,c.Kind,c.Status,c.IsDemo,c.IssuedAt,c.RevokedAt,c.Sha256,institution,note="Verification of this stored document record; not a digital signature or external university attestation."});
        }).RequireRateLimiting("verification");
    }
    private static async Task<IResult> List(ErpDbContext db,Guid institution,Guid student) => Results.Ok(await db.AcademicCertificates.Where(x=>x.InstitutionId==institution&&x.StudentId==student).OrderByDescending(x=>x.RequestedAt).Take(100).Select(x=>new{x.Id,x.Kind,x.Status,x.Number,x.RequestedAt,x.IssuedAt,x.RevokedAt,x.Version,x.IsDemo,x.Sha256,verificationPath=x.VerificationToken==null?null:"/verify/"+x.VerificationToken}).ToListAsync());
    private static async Task<IResult> Download(ErpDbContext db,Guid institution,Guid id)
    {
        var c=await db.AcademicCertificates.AsNoTracking().SingleOrDefaultAsync(x=>x.InstitutionId==institution&&x.Id==id);return c?.Status=="Issued"&&c.Pdf!=null?Results.File(c.Pdf,"application/pdf",c.Number+".pdf"):Results.NotFound();
    }
    private static async Task<IResult> Request(ErpDbContext db,HttpContext ctx,Guid institution,Guid studentId,CertificateRequest r,bool demo)
    {
        if(r.Kind is not("Transcript" or "CourseCompletion")||(r.Kind=="Transcript"&&r.GraduationPolicyId!=null)||(r.Kind=="CourseCompletion"&&(!Text(r.ClearanceReference)||r.GraduationPolicyId==null)))return Results.Problem("Select Transcript, or CourseCompletion with an approved curriculum and finance/library/administration clearance evidence reference.",statusCode:400);
        await using var tx=await db.Database.BeginTransactionAsync();var student=(await db.Students.FromSqlInterpolated($"SELECT * FROM \"Students\" WHERE \"InstitutionId\"={institution} AND \"Id\"={studentId} FOR UPDATE").ToListAsync()).SingleOrDefault();if(student==null||student.Status=="Archived")return Results.NotFound();
        if(!await db.ExamMarks.AnyAsync(x=>x.InstitutionId==institution&&x.StudentId==studentId&&x.IsPublished))return Results.Problem("No published academic results are available.",statusCode:409);
        if(await db.AcademicCertificates.AnyAsync(x=>x.InstitutionId==institution&&x.StudentId==studentId&&x.Kind==r.Kind&&x.Status=="Requested"))return Results.Problem("A request of this kind is already awaiting review.",statusCode:409);
        if(r.Kind=="CourseCompletion")
        {
            var policy=await db.GraduationPolicies.SingleOrDefaultAsync(x=>x.InstitutionId==institution&&x.Id==r.GraduationPolicyId);if(policy==null)return Results.Problem("Curriculum is unavailable.",statusCode:400);var evaluation=await Evaluate(db,institution,studentId,policy);if(!evaluation.Eligible)return Results.Problem(string.Join(" ",evaluation.Blockers),statusCode:409);
        }
        var certificate=new AcademicCertificate{InstitutionId=institution,StudentId=studentId,Kind=r.Kind,GraduationPolicyId=r.GraduationPolicyId,ClearanceReference=r.ClearanceReference?.Trim()??"",RequestedBy=ctx.Actor(),SnapshotJson=await Snapshot(db,institution,student),IsDemo=demo||student.IsDemo};db.AcademicCertificates.Add(certificate);Audit(db,ctx,institution,"Requested",certificate.Id,r.Kind+" immutable review snapshot.");await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/v1/institutions/{institution}/certificates/students/{studentId}",new{certificate.Id,certificate.Status,certificate.Version});
    }
    private static async Task<string> Snapshot(ErpDbContext db,Guid institution,Student student)
    {
        var name=await db.Institutions.Where(x=>x.Id==institution).Select(x=>x.Name).SingleAsync();return JsonSerializer.Serialize(new{institution=name,student=student.FullName,studentNumber=student.StudentNumber,results=await ExaminationEndpoints.Transcript(db,institution,student.Id)});
    }
    internal static async Task<GraduationEvaluation> Evaluate(ErpDbContext db,Guid institution,Guid student,GraduationPolicy policy)
    {
        var blockers=new List<string>();if(policy.Status!="Approved")blockers.Add("Curriculum is not independently approved.");
        if(!await db.Enrollments.AnyAsync(x=>x.InstitutionId==institution&&x.StudentId==student&&x.ProgrammeId==policy.ProgrammeId&&x.AcademicSessionId==policy.AcademicSessionId))blockers.Add("No matching programme/session enrollment.");
        var required=JsonSerializer.Deserialize<Guid[]>(policy.RequiredSubjectsJson)!;
        var results=await(from m in db.ExamMarks join e in db.ExamAssessments on new{m.InstitutionId,Id=m.AssessmentId} equals new{e.InstitutionId,e.Id} join s in db.Subjects on new{m.InstitutionId,Id=m.SubjectId} equals new{s.InstitutionId,s.Id} where m.InstitutionId==institution&&m.StudentId==student&&m.IsPublished&&e.Status=="Published"&&s.ProgrammeId==policy.ProgrammeId&&s.AcademicSessionId==policy.AcademicSessionId select new{m.SubjectId,m.IsPass,e.Credits}).ToListAsync();
        var passed=results.Where(x=>x.IsPass&&required.Contains(x.SubjectId)).ToList();var credits=passed.Sum(x=>x.Credits);
        if(passed.Count!=required.Length)blockers.Add("Required curriculum subjects are missing or not passed.");if(results.Any(x=>!x.IsPass))blockers.Add("Published backlogs remain for this programme/session.");if(credits<policy.MinimumCredits)blockers.Add("Passed curriculum credits are below the approved minimum.");
        var chargeIds=await db.AdmissionApplications.Where(x=>x.InstitutionId==institution&&x.StudentId==student).Select(x=>x.Id).ToListAsync();
        var charges=await db.AdmissionCharges.Where(x=>x.InstitutionId==institution&&chargeIds.Contains(x.ApplicationId)&&!x.IsVoid).Select(x=>new{x.Id,x.Amount}).ToListAsync();
        foreach(var charge in charges){var confirmed=await db.AdmissionPayments.Where(x=>x.InstitutionId==institution&&x.ChargeId==charge.Id&&x.Status=="Confirmed").SumAsync(x=>(decimal?)x.Amount)??0;if(confirmed<charge.Amount){blockers.Add("Recorded admission dues remain unpaid.");break;}}
        return new(blockers.Count==0,credits,required.Length,passed.Count,blockers.ToArray());
    }
}
