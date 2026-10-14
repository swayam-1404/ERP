using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
namespace VHEI.Infrastructure;
public static class DemoSeed
{
    public const int RandomSeed=20261014;
    private static Guid Id(string value)=>new(SHA256.HashData(Encoding.UTF8.GetBytes("VHEI-DEMO:"+value)).AsSpan(0,16));
    public static async Task RunAsync(ErpDbContext db)
    {
        var random=new Random(RandomSeed);
        var institutions=await db.Institutions.OrderBy(x=>x.Code).ToListAsync();
        string[] first=["Aarav","Ananya","Ishan","Kavya","Rohan","Meera","Samar","Nisha","Aditya","Diya","Vihaan","Avani","Arjun","Tara","Neel","Riya"];
        string[] last=["DemoPatra","DemoNayak","DemoSahu","DemoDas","DemoMishra","DemoPanda","DemoSen","DemoRao"];
        var programmes=await db.Programmes.OrderBy(x=>x.Name).ToListAsync();var sessions=await db.AcademicSessions.ToListAsync();
        var existing=(await db.Students.Where(x=>x.IsDemo).Select(x=>x.Id).ToListAsync()).ToHashSet();
        for(int i=0;i<500;i++)
        {
            var institution=institutions[i%institutions.Count];var id=Id("student:"+i);var fullName=first[random.Next(first.Length)]+" "+last[random.Next(last.Length)];
            if(existing.Contains(id))continue;
            var choices=programmes.Where(p=>p.InstitutionId==institution.Id).ToList();
            var guardian=new Guardian{Id=Id("guardian:"+i),InstitutionId=institution.Id,FullName="Guardian "+(i+1).ToString("D3")+" Demo",Email=$"guardian{i+1:D4}@example.test",IsDemo=true};
            var student=new Student{Id=id,InstitutionId=institution.Id,StudentNumber=$"DEMO-{institution.Code}-{i+1:D4}",FullName=fullName,Email=$"student{i+1:D4}@example.test",GuardianId=guardian.Id,Status=i%20==0?"Alumni":i%17==0?"Graduated":i%13==0?"NewlyAdmitted":"Active",IsDemo=true,CreatedAt=new DateTime(2026,7,1,0,0,0,DateTimeKind.Utc).AddDays(i%90)};
            db.Guardians.Add(guardian);db.Students.Add(student);db.Enrollments.Add(new Enrollment{Id=Id("enrollment:"+i),InstitutionId=institution.Id,StudentId=id,ProgrammeId=choices[(i/institutions.Count)%choices.Count].Id,AcademicSessionId=sessions.Single(s=>s.InstitutionId==institution.Id).Id,BatchYear=2023+i%4,Semester=1+2*(i%4),Section=i%2==0?"A":"B",IsDemo=true});
        }
        foreach(var institution in institutions)
        {
            var departments=await db.Departments.Where(x=>x.InstitutionId==institution.Id).OrderBy(x=>x.Name).ToListAsync();
            for(int i=0;i<4;i++)
            {
                var id=Id("faculty:"+institution.Code+":"+i);if(await db.Faculty.AnyAsync(x=>x.Id==id))continue;
                db.Faculty.Add(new Faculty{Id=id,InstitutionId=institution.Id,FullName=first[(i+3)%first.Length]+" "+last[i%last.Length],Email=$"faculty-{institution.Code.ToLowerInvariant()}-{i+1}@example.test",Designation="DEMO Lecturer",DepartmentId=departments.Count>0?departments[i%departments.Count].Id:null,IsDemo=true});
            }
        }
        await db.SaveChangesAsync();
    }
}
