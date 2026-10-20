using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
namespace VHEI.Infrastructure;
public static class PortalDemoSeed
{
    public static async Task RunAsync(ErpDbContext db)
    {
        var vit=await db.Institutions.SingleAsync(x=>x.Code=="VIT");
        var student=await db.Students.Where(x=>x.InstitutionId==vit.Id&&x.IsDemo&&x.StudentNumber.StartsWith("DEMO-VIT-")&&x.Status=="Active").OrderBy(x=>x.StudentNumber).FirstAsync();
        var actor=await db.Users.Where(x=>x.Email=="admin@example.test").Select(x=>x.Id).SingleAsync();
        foreach(var pair in new[]{("student@example.test","Student"),("guardian@example.test","Guardian")})
        {
            var user=await db.Users.SingleAsync(x=>x.Email==pair.Item1);
            if(await db.PortalAccesses.AnyAsync(x=>x.InstitutionId==vit.Id&&x.UserId==user.Id))continue;
            var access=new PortalAccess{InstitutionId=vit.Id,UserId=user.Id,StudentId=student.Id,Kind=pair.Item2,GrantedBy=actor,EvidenceReference="Fixed synthetic portal demonstration link; never real identity verification."};
            db.PortalAccesses.Add(access);db.AuditLogs.Add(new AuditLog{InstitutionId=vit.Id,ActorId=actor,Action="Demo.PortalAccessLinked",Resource=access.Id.ToString(),Details="Synthetic student/guardian portal linked to a synthetic VIT student."});
        }
        await db.SaveChangesAsync();
    }
}
