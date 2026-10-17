using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;

public record RosterMember(Guid Id,Guid StudentId);
public record SchedulingContext(Subject Subject,Classroom Room,string Section,List<RosterMember> Roster);
public static class ClassScheduling
{
    public static async Task<(SchedulingContext? Value,string? Error)> Prepare(ErpDbContext db,Guid institution,Guid subjectId,Guid facultyId,Guid classroomId,string? section)
    {
        if(string.IsNullOrWhiteSpace(section)||section.Trim().Length>12)return(null,"Provide a section up to 12 characters.");
        var subject=await db.Subjects.SingleOrDefaultAsync(x=>x.InstitutionId==institution&&x.Id==subjectId);
        var room=await db.Classrooms.SingleOrDefaultAsync(x=>x.InstitutionId==institution&&x.Id==classroomId);
        if(subject==null||room==null||!await db.Faculty.AnyAsync(x=>x.InstitutionId==institution&&x.Id==facultyId&&!x.IsArchived))return(null,"Subject, room and active faculty must belong to this institution.");
        var normalized=section.Trim().ToUpperInvariant();
        var roster=await(from e in db.Enrollments join student in db.Students on new{e.InstitutionId,Id=e.StudentId} equals new{student.InstitutionId,student.Id} where e.InstitutionId==institution&&e.ProgrammeId==subject.ProgrammeId&&e.AcademicSessionId==subject.AcademicSessionId&&e.Semester==subject.Semester&&e.Section==normalized&&(student.Status=="Active"||student.Status=="NewlyAdmitted") select new RosterMember(e.Id,e.StudentId)).ToListAsync();
        if(roster.Count==0)return(null,"No active enrolled students match this subject semester and section.");
        if(roster.Count>room.Capacity)return(null,"Roster exceeds room capacity.");
        return(new(subject,room,normalized,roster),null);
    }
    public static ClassSession AddPeriod(ErpDbContext db,SchedulingContext prepared,Guid faculty,DateTime start,DateTime end,bool demo,Guid? series=null)
    {
        var subject=prepared.Subject;
        var session=new ClassSession{InstitutionId=subject.InstitutionId,TimetableSeriesId=series,SubjectId=subject.Id,ProgrammeId=subject.ProgrammeId,AcademicSessionId=subject.AcademicSessionId,Semester=subject.Semester,FacultyId=faculty,ClassroomId=prepared.Room.Id,Section=prepared.Section,StartsAt=start,EndsAt=end,IsDemo=demo};
        db.ClassSessions.Add(session);
        foreach(var member in prepared.Roster)db.ClassRosters.Add(new ClassRoster{InstitutionId=session.InstitutionId,ClassSessionId=session.Id,EnrollmentId=member.Id,StudentId=member.StudentId});
        return session;
    }
}
