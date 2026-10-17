using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;

public record FeePlanRequest(string Name,Guid ProgrammeId,Guid AcademicSessionId,string SourceAcademicYear,string SourceReference,string AuthorityReference,decimal Amount,string CollectionMode="Offline");
public record ChargeRequest(int Version,Guid FeePlanId);
public record OfflinePaymentRequest(Guid IdempotencyKey,string ExternalReference,string EvidenceReference,string Method,DateOnly PaidOn,decimal Amount);
public record FinalAdmissionRequest(int Version,string ApprovalReference,string Reason);
public record AdmissionEnrollmentRequest(int Version,string StudentNumber,string GuardianName,string GuardianEmail,int BatchYear,int Semester,string Section);

public static class AdmissionFinanceEndpoints
{
    private static bool Text(string? value,int min,int max)=>value!=null&&value.Trim().Length>=min&&value.Length<=max;
    private static bool Money(decimal value)=>value>0&&value<=10000000m&&decimal.Round(value,2)==value;
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(330));
    private static string Year(string s)=>s.Replace('–','-').Trim();
    private static Task<bool> RequiresSams(ErpDbContext db,Guid institution,Guid programme)=>db.Programmes.AnyAsync(x=>x.InstitutionId==institution&&x.Id==programme&&x.Name.StartsWith("+3")&&db.Institutions.Any(i=>i.Id==institution&&i.Code=="VDC"));
    private static void Audit(ErpDbContext db,Guid institution,Guid actor,string action,Guid resource,string note)=>db.AuditLogs.Add(new AuditLog{InstitutionId=institution,ActorId=actor,Action="Finance."+action,Resource=resource.ToString(),Details=note});
    private static async Task<decimal> Confirmed(ErpDbContext db,Guid institution,Guid charge)=>await db.AdmissionPayments.Where(x=>x.InstitutionId==institution&&x.ChargeId==charge&&x.Status=="Confirmed").SumAsync(x=>(decimal?)x.Amount)??0m;
    public static void MapAdmissionFinance(this RouteGroupBuilder group,WebApplication app)
    {
        var finance=group.MapGroup("/finance").RequireAuthorization("Manage");
        finance.MapGet("/fee-plans",async(Guid institutionId,Guid? programmeId,Guid? sessionId,string? status,int? page,int? pageSize,ErpDbContext db)=>
        {
            if(status!=null&&status is not("Draft" or "Approved"))return Results.Problem("Invalid fee plan status.",statusCode:400);
            var query=db.AdmissionFeePlans.Where(x=>x.InstitutionId==institutionId&&(programmeId==null||x.ProgrammeId==programmeId)&&(sessionId==null||x.AcademicSessionId==sessionId)&&(status==null||x.Status==status));
            var p=Math.Clamp(page??1,1,10000);var size=Math.Clamp(pageSize??25,1,100);
            return Results.Ok(new{items=await query.OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id).Skip((p-1)*size).Take(size).ToListAsync(),total=await query.CountAsync(),page=p,pageSize=size});
        });
        finance.MapPost("/fee-plans",async(Guid institutionId,FeePlanRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Name,3,120)||!Text(r.SourceAcademicYear,4,20)||!Text(r.SourceReference,3,500)||!Text(r.AuthorityReference,3,500)||!Money(r.Amount)||r.CollectionMode is not("Offline" or "ExternalPortal"))return Results.Problem("Provide the fee plan name, source year, fee/authority references, collection mode and a positive INR amount with at most two decimal places (maximum 10,000,000).",statusCode:400);
            if(!await db.Programmes.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.ProgrammeId)||!await db.AcademicSessions.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==r.AcademicSessionId))return Results.Problem("Programme and session must belong to this institution.",statusCode:400);
            var plan=new AdmissionFeePlan{InstitutionId=institutionId,Name=r.Name.Trim(),ProgrammeId=r.ProgrammeId,AcademicSessionId=r.AcademicSessionId,SourceAcademicYear=r.SourceAcademicYear.Trim(),SourceReference=r.SourceReference.Trim(),AuthorityReference=r.AuthorityReference.Trim(),Amount=r.Amount,CollectionMode=r.CollectionMode,CreatedBy=ctx.Actor(),IsDemo=app.Environment.IsDevelopment()};
            db.AdmissionFeePlans.Add(plan);Audit(db,institutionId,ctx.Actor(),"FeePlanCreated",plan.Id,"Draft admission charge plan; not approved.");await db.SaveChangesAsync();return Results.Created($"/api/v1/institutions/{institutionId}/finance/fee-plans",plan);
        });
        finance.MapPost("/fee-plans/{id:guid}/approve",async(Guid institutionId,Guid id,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Reason,3,500))return Results.Problem("Provide an approval note of 3–500 characters.",statusCode:400);
            var plan=await db.AdmissionFeePlans.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(plan==null)return Results.NotFound();
            if(plan.Status!="Draft"||plan.Version!=r.Version)return Results.Problem("Plan changed or already approved.",statusCode:409);
            if(plan.CreatedBy==ctx.Actor())return Results.Problem("Another administrator must approve the fee plan.",statusCode:403);
            var session=await db.AcademicSessions.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==plan.AcademicSessionId);
            if(Year(plan.SourceAcademicYear)!=Year(session.Name))return Results.Problem("The source academic year must match the selected session. Historical fees cannot be activated for another year.",statusCode:409);
            if(await RequiresSams(db,institutionId,plan.ProgrammeId)&&plan.CollectionMode!="ExternalPortal")return Results.Problem("VDC +3 admissions require external SAMS selection/payment evidence. Use an external-portal plan.",statusCode:409);
            plan.Status="Approved";plan.ApprovedBy=ctx.Actor();plan.ApprovalNote=r.Reason.Trim();plan.Version++;Audit(db,institutionId,ctx.Actor(),"FeePlanApproved",id,"Independent approval of the referenced admission charge plan.");
            try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Results.Problem("Plan changed. Reload before approving.",statusCode:409);}return Results.Ok(plan);
        });
        var admissions=group.MapGroup("/admissions/{applicationId:guid}").RequireAuthorization("Manage");
        admissions.MapGet("/finance",async(Guid institutionId,Guid applicationId,ErpDbContext db)=>
        {
            if(!await db.AdmissionApplications.AnyAsync(x=>x.InstitutionId==institutionId&&x.Id==applicationId))return Results.NotFound();
            var charge=await db.AdmissionCharges.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.ApplicationId==applicationId);
            var payments=charge==null?new List<AdmissionPayment>():await db.AdmissionPayments.Where(x=>x.InstitutionId==institutionId&&x.ChargeId==charge.Id).OrderBy(x=>x.RecordedAt).ToListAsync();
            return Results.Ok(new{charge,plan=charge==null?null:await db.AdmissionFeePlans.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==charge.FeePlanId),payments,confirmed=charge==null?0m:await Confirmed(db,institutionId,charge.Id)});
        });
        admissions.MapPost("/charge",async(Guid institutionId,Guid applicationId,ChargeRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            await using var tx=await db.Database.BeginTransactionAsync();var a=await AdmissionEndpoints.Lock(db,institutionId,applicationId);if(a==null)return Results.NotFound();
            if(a.Status!="Reviewed"||a.Version!=r.Version)return Results.Problem("Only the current reviewed application can receive a charge.",statusCode:409);
            if(await db.AdmissionCharges.AnyAsync(x=>x.InstitutionId==institutionId&&x.ApplicationId==applicationId))return Results.Problem("A charge already exists for this application.",statusCode:409);
            var plan=await db.AdmissionFeePlans.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==r.FeePlanId&&x.ProgrammeId==a.ProgrammeId&&x.AcademicSessionId==a.AcademicSessionId&&x.Status=="Approved");if(plan==null)return Results.Problem("Choose an independently approved fee plan for this programme and session.",statusCode:400);
            if(plan.IsDemo!=a.IsDemo)return Results.Problem("Demo and live financial records cannot be mixed.",statusCode:409);
            var charge=new AdmissionCharge{InstitutionId=institutionId,ApplicationId=a.Id,FeePlanId=plan.Id,Amount=plan.Amount};db.AdmissionCharges.Add(charge);a.Version++;AdmissionEndpoints.Event(db,a,ctx.Actor(),"ChargeCreated","Admission charge issued against an approved plan.");Audit(db,institutionId,ctx.Actor(),"ChargeCreated",charge.Id,$"Application {a.Id}; plan {plan.Id}.");await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/v1/institutions/{institutionId}/admissions/{a.Id}/finance",charge);
        });
        admissions.MapPost("/payments",async(Guid institutionId,Guid applicationId,OfflinePaymentRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(r.IdempotencyKey==Guid.Empty||!Money(r.Amount)||!Text(r.ExternalReference,3,120)||!r.ExternalReference.All(c=>char.IsLetterOrDigit(c)||c is '-' or '/' or '_' or '.')||!Text(r.EvidenceReference,3,500)||r.Method is not("Cash" or "BankTransfer" or "Cheque" or "ExternalPortal")||r.PaidOn.Year<2000||r.PaidOn>Today)return Results.Problem("Provide a positive two-decimal amount, unique reference (letters, digits, hyphens, slashes, underscores or dots), evidence, supported method and payment date no later than today.",statusCode:400);
            await using var tx=await db.Database.BeginTransactionAsync();var a=await AdmissionEndpoints.Lock(db,institutionId,applicationId);if(a==null)return Results.NotFound();
            var charge=await db.AdmissionCharges.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.ApplicationId==applicationId);if(charge==null)return Results.Problem("Issue an approved admission charge first.",statusCode:409);
            var reference=r.ExternalReference.Trim().ToUpperInvariant();var prior=await db.AdmissionPayments.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.IdempotencyKey==r.IdempotencyKey);
            if(prior!=null)return prior.ChargeId==charge.Id&&prior.Amount==r.Amount&&prior.ExternalReference==reference&&prior.EvidenceReference==r.EvidenceReference.Trim()&&prior.Method==r.Method&&prior.PaidOn==r.PaidOn?Results.Ok(prior):Results.Problem("This idempotency key belongs to a different payment request.",statusCode:409);
            if(a.Status!="Reviewed"||charge.IsVoid)return Results.Problem("Payments can only be recorded for an active reviewed application charge.",statusCode:409);
            var plan=await db.AdmissionFeePlans.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==charge.FeePlanId);
            if((plan.CollectionMode=="ExternalPortal")!=(r.Method=="ExternalPortal"))return Results.Problem("The payment method must match the approved plan's external-portal or offline collection mode.",statusCode:400);
            var reserved=await db.AdmissionPayments.Where(x=>x.InstitutionId==institutionId&&x.ChargeId==charge.Id&&(x.Status=="Pending"||x.Status=="Confirmed")).SumAsync(x=>(decimal?)x.Amount)??0m;
            if(reserved+r.Amount>charge.Amount)return Results.Problem("Payment exceeds the remaining amount after confirmed and pending records.",statusCode:409);
            var payment=new AdmissionPayment{InstitutionId=institutionId,ChargeId=charge.Id,IdempotencyKey=r.IdempotencyKey,ExternalReference=reference,EvidenceReference=r.EvidenceReference.Trim(),Method=r.Method,PaidOn=r.PaidOn,Amount=r.Amount,RecordedBy=ctx.Actor()};db.AdmissionPayments.Add(payment);Audit(db,institutionId,ctx.Actor(),"PaymentRecorded",payment.Id,"Offline payment reference recorded; independent confirmation required.");
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("The payment reference or request key already exists.",statusCode:409);}await tx.CommitAsync();return Results.Created($"/api/v1/institutions/{institutionId}/admissions/{a.Id}/finance",payment);
        });
        MapPaymentReview(finance);
        MapAdmissionConversion(admissions,app);
        MapReports(finance);
    }
    private static void MapPaymentReview(RouteGroupBuilder routes)
    {
        routes.MapPost("/payments/{id:guid}/review",async(Guid institutionId,Guid id,ReviewRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.Note,3,500))return Results.Problem("Provide an evidence review note of 3–500 characters.",statusCode:400);
            var chargeId=await db.AdmissionPayments.Where(x=>x.InstitutionId==institutionId&&x.Id==id).Select(x=>(Guid?)x.ChargeId).SingleOrDefaultAsync();if(chargeId==null)return Results.NotFound();var applicationId=await db.AdmissionCharges.Where(x=>x.InstitutionId==institutionId&&x.Id==chargeId).Select(x=>x.ApplicationId).SingleAsync();
            await using var tx=await db.Database.BeginTransactionAsync();var a=await AdmissionEndpoints.Lock(db,institutionId,applicationId);var payment=await db.AdmissionPayments.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==id);var charge=await db.AdmissionCharges.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==payment.ChargeId);
            if(payment.Status!="Pending"||a?.Status!="Reviewed"||charge.IsVoid)return Results.Problem("Payment already reviewed or application/charge no longer accepts confirmation.",statusCode:409);
            if(payment.RecordedBy==ctx.Actor())return Results.Problem("Another administrator must verify this payment against its evidence.",statusCode:403);
            if(r.Approve&&await Confirmed(db,institutionId,charge.Id)+payment.Amount>charge.Amount)return Results.Problem("Confirmation would overpay the charge.",statusCode:409);
            payment.Status=r.Approve?"Confirmed":"Rejected";payment.ReviewedBy=ctx.Actor();payment.ReviewNote=r.Note.Trim();if(r.Approve)payment.AcknowledgementNumber="ACK-"+Guid.NewGuid().ToString("N").ToUpperInvariant();Audit(db,institutionId,ctx.Actor(),"Payment"+payment.Status,id,"Independent review of recorded offline payment evidence.");await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(payment);
        });
        routes.MapGet("/payments/{id:guid}/acknowledgement",async(Guid institutionId,Guid id,ErpDbContext db)=>
        {
            var payment=await db.AdmissionPayments.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id&&x.Status=="Confirmed");if(payment==null)return Results.NotFound();
            var charge=await db.AdmissionCharges.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==payment.ChargeId);var a=await db.AdmissionApplications.SingleAsync(x=>x.InstitutionId==institutionId&&x.Id==charge.ApplicationId);var institution=await db.Institutions.SingleAsync(x=>x.Id==institutionId);
            var text=$"{institution.Name}\n{(a.IsDemo?"DEMO — SYNTHETIC PAYMENT RECORD":"OFFLINE PAYMENT ACKNOWLEDGEMENT")}\nAcknowledgement: {payment.AcknowledgementNumber}\nApplication: {a.ApplicationNumber}\nRecorded amount: INR {payment.Amount:F2}\nPayment date: {payment.PaidOn:yyyy-MM-dd}\nMethod: {payment.Method}\nExternal reference: {payment.ExternalReference}\nIndependently checked against recorded evidence.\nThis is an ERP acknowledgement of an offline record, not a bank settlement confirmation or tax invoice.\n";
            return Results.File(Encoding.UTF8.GetBytes(text),"text/plain; charset=utf-8",payment.AcknowledgementNumber+".txt");
        });
    }
    private static void MapAdmissionConversion(RouteGroupBuilder routes,WebApplication app)
    {
        routes.MapPost("/approve",async(Guid institutionId,Guid applicationId,FinalAdmissionRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.ApprovalReference,3,500)||!Text(r.Reason,3,500))return Results.Problem("Record final admission authority/reference and an approval reason.",statusCode:400);
            await using var tx=await db.Database.BeginTransactionAsync();var a=await AdmissionEndpoints.Lock(db,institutionId,applicationId);if(a==null)return Results.NotFound();if(a.Status!="Reviewed"||a.Version!=r.Version)return Results.Problem("Only the current reviewed application can be approved.",statusCode:409);if(a.SubmittedBy==ctx.Actor())return Results.Problem("The submitting administrator cannot give final approval.",statusCode:403);
            if(await RequiresSams(db,institutionId,a.ProgrammeId)&&!Text(a.ExternalReference,3,120))return Results.Problem("Record and independently review the SAMS selection/intimation reference before approval.",statusCode:409);
            var charge=await db.AdmissionCharges.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.ApplicationId==applicationId&&!x.IsVoid);if(charge==null||await Confirmed(db,institutionId,charge.Id)!=charge.Amount)return Results.Problem("The admission charge must be fully confirmed before final approval.",statusCode:409);
            a.Status="Approved";a.ApprovedBy=ctx.Actor();a.ApprovalReference=r.ApprovalReference.Trim();a.Version++;AdmissionEndpoints.Event(db,a,ctx.Actor(),"Approved",r.Reason.Trim());await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(a);
        });
        routes.MapPost("/enroll",async(Guid institutionId,Guid applicationId,AdmissionEnrollmentRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(!Text(r.StudentNumber,1,40)||!r.StudentNumber.All(c=>char.IsLetterOrDigit(c)||c is '-' or '/')||!Text(r.GuardianName,2,120)||!StudentEndpoints.EmailValid(r.GuardianEmail)||r.BatchYear is <2000 or >2100||r.Semester is <1 or >12||!Text(r.Section,1,12))return Results.Problem("Provide a valid student number, guardian, batch, semester and section.",statusCode:400);
            await using var tx=await db.Database.BeginTransactionAsync();var a=await AdmissionEndpoints.Lock(db,institutionId,applicationId);if(a==null)return Results.NotFound();if(a.Status!="Approved"||a.StudentId!=null||a.Version!=r.Version)return Results.Problem("Only the current approved application can be enrolled once.",statusCode:409);
            var charge=await db.AdmissionCharges.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.ApplicationId==applicationId&&!x.IsVoid);if(charge==null||await Confirmed(db,institutionId,charge.Id)!=charge.Amount)return Results.Problem("Confirmed admission payment is required.",statusCode:409);
            if(a.IsDemo!=app.Environment.IsDevelopment())return Results.Problem("Demo and live enrollment modes cannot be mixed.",statusCode:409);
            var guardian=new Guardian{InstitutionId=institutionId,FullName=r.GuardianName.Trim(),Email=r.GuardianEmail.Trim(),IsDemo=a.IsDemo};var student=new Student{InstitutionId=institutionId,StudentNumber=r.StudentNumber.Trim().ToUpperInvariant(),FullName=a.FullName,Email=a.Email,GuardianId=guardian.Id,Status="NewlyAdmitted",IsDemo=a.IsDemo};
            db.Guardians.Add(guardian);db.Students.Add(student);db.Enrollments.Add(new Enrollment{InstitutionId=institutionId,StudentId=student.Id,ProgrammeId=a.ProgrammeId,AcademicSessionId=a.AcademicSessionId,BatchYear=r.BatchYear,Semester=r.Semester,Section=r.Section.Trim().ToUpperInvariant(),IsDemo=a.IsDemo});a.StudentId=student.Id;a.Status="Enrolled";a.Version++;AdmissionEndpoints.Event(db,a,ctx.Actor(),"Enrolled","Approved application converted to student, guardian and enrollment records.");db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="Student.CreatedFromAdmission",Resource=student.Id.ToString(),Details=$"Application {a.Id}."});
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){return Results.Problem("Student number already exists. No part of the conversion was saved.",statusCode:409);}await tx.CommitAsync();return Results.Created($"/api/v1/institutions/{institutionId}/students/{student.Id}",new{studentId=student.Id,applicationVersion=a.Version});
        });
    }
    private static void MapReports(RouteGroupBuilder routes)
    {
        routes.MapGet("/charges",async(Guid institutionId,string? search,int? page,ErpDbContext db)=>
        {
            if(search?.Length>100)return Results.Problem("Search must be at most 100 characters.",statusCode:400);var term="%"+(search?.Trim()??"")+"%";
            var query=from c in db.AdmissionCharges join a in db.AdmissionApplications on new{c.InstitutionId,Id=c.ApplicationId} equals new{a.InstitutionId,a.Id} where c.InstitutionId==institutionId&&(EF.Functions.ILike(a.ApplicationNumber,term)||EF.Functions.ILike(a.FullName,term)) select new{c.Id,c.ApplicationId,a.ApplicationNumber,a.FullName,c.Amount,c.Currency,c.IsVoid,confirmed=db.AdmissionPayments.Where(p=>p.InstitutionId==institutionId&&p.ChargeId==c.Id&&p.Status=="Confirmed").Sum(p=>(decimal?)p.Amount)??0m,pending=db.AdmissionPayments.Where(p=>p.InstitutionId==institutionId&&p.ChargeId==c.Id&&p.Status=="Pending").Sum(p=>(decimal?)p.Amount)??0m};
            var p=Math.Clamp(page??1,1,10000);return Results.Ok(new{items=await query.OrderBy(x=>x.ApplicationNumber).Skip((p-1)*25).Take(25).ToListAsync(),total=await query.CountAsync(),page=p});
        });
    }
}
