using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
namespace VHEI.Infrastructure;
public static class AcademicDemoSeed
{
    public static async Task RunAsync(ErpDbContext db)
    {
        foreach(var institution in await db.Institutions.ToListAsync())
        {
            if(!await db.Classrooms.AnyAsync(x=>x.InstitutionId==institution.Id))db.Classrooms.Add(new Classroom{InstitutionId=institution.Id,Name="DEMO Classroom 101",Capacity=80,IsDemo=true});
            var enrollment=await(from e in db.Enrollments join s in db.Students on e.StudentId equals s.Id where e.InstitutionId==institution.Id&&s.IsDemo&&s.Status=="Active" orderby s.StudentNumber select e).FirstAsync();
            if(!await db.Subjects.AnyAsync(x=>x.InstitutionId==institution.Id))db.Subjects.Add(new Subject{InstitutionId=institution.Id,Code="DEMO-101",Name="DEMO Foundation Studies",ProgrammeId=enrollment.ProgrammeId,AcademicSessionId=enrollment.AcademicSessionId,Semester=enrollment.Semester,IsDemo=true});
            if(institution.Code=="VIT")
            {
                var user=await db.Users.SingleAsync(x=>x.Email=="faculty@example.test");
                var faculty=await db.Faculty.SingleAsync(x=>x.InstitutionId==institution.Id&&x.IsDemo&&x.Email=="faculty-vit-1@example.test");
                var linked=await db.Faculty.SingleOrDefaultAsync(x=>x.InstitutionId==institution.Id&&x.UserId==user.Id);
                if(linked?.Id!=faculty.Id)
                {
                    if(linked!=null&&!linked.IsDemo)throw new InvalidOperationException("Demo seed refuses to relink non-demo faculty.");
                    await using var tx=await db.Database.BeginTransactionAsync();
                    if(linked!=null){linked.UserId=null;await db.SaveChangesAsync();}
                    faculty.UserId=user.Id;
                    db.AuditLogs.Add(new AuditLog{InstitutionId=institution.Id,ActorId=(await db.Users.SingleAsync(x=>x.Email=="admin@example.test")).Id,Action="Demo.FacultyAccountLinked",Resource=faculty.Id.ToString(),Details="Development portal linked to the fixed seeded faculty profile; earlier synthetic association replaced if present."});
                    await db.SaveChangesAsync();await tx.CommitAsync();
                }
            }
        }
        await db.SaveChangesAsync();
    }
}
