using Microsoft.EntityFrameworkCore;
using Npgsql;
using VHEI.Domain;
using VHEI.Infrastructure;
namespace VHEI.Api;

public record SeriesRequest(string Name,Guid SubjectId,Guid FacultyId,Guid ClassroomId,string Section,DateOnly FirstDate,DateOnly LastDate,TimeOnly StartTime,TimeOnly EndTime,int[] DaysOfWeek,DateOnly[] ExcludedDates);
public record PlannedPeriod(DateTime StartsAt,DateTime EndsAt,string[] Conflicts);
public static class RecurringTimetableEndpoints
{
    // Campus time is fixed IST. Explicit offset avoids dependence on the host timezone.
    private static readonly TimeSpan CampusOffset=TimeSpan.FromMinutes(330);
    private static (List<PlannedPeriod>? Periods,string? Error) Expand(SeriesRequest r)
    {
        if(string.IsNullOrWhiteSpace(r.Name)||r.Name.Trim().Length>120)return(null,"Enter a series name up to 120 characters.");
        if(r.FirstDate.Year<2000||r.LastDate.Year>2100||r.LastDate<r.FirstDate||r.LastDate.DayNumber-r.FirstDate.DayNumber>111)return(null,"Choose a range from 1 to 112 days, within years 2000–2100.");
        if(r.EndTime<=r.StartTime||r.EndTime-r.StartTime<TimeSpan.FromMinutes(1)||r.EndTime-r.StartTime>TimeSpan.FromHours(4))return(null,"Use a same-day period from 1 minute to 4 hours.");
        if(r.DaysOfWeek==null||r.DaysOfWeek.Length is <1 or >7||r.DaysOfWeek.Any(d=>d is <0 or >6)||r.DaysOfWeek.Distinct().Count()!=r.DaysOfWeek.Length)return(null,"Select distinct weekdays (Sunday=0 through Saturday=6).");
        if(r.ExcludedDates==null||r.ExcludedDates.Length>112||r.ExcludedDates.Distinct().Count()!=r.ExcludedDates.Length||r.ExcludedDates.Any(d=>d<r.FirstDate||d>r.LastDate||!r.DaysOfWeek.Contains((int)d.DayOfWeek)))return(null,"Skipped dates must be distinct scheduled weekdays within this series.");
        var periods=new List<PlannedPeriod>();
        for(var date=r.FirstDate;date<=r.LastDate;date=date.AddDays(1))
        {
            if(!r.DaysOfWeek.Contains((int)date.DayOfWeek)||r.ExcludedDates.Contains(date))continue;
            periods.Add(new(new DateTimeOffset(date.ToDateTime(r.StartTime),CampusOffset).UtcDateTime,new DateTimeOffset(date.ToDateTime(r.EndTime),CampusOffset).UtcDateTime,[]));
        }
        return periods.Count is <1 or >64?(null,"A series must generate from 1 to 64 periods. Reduce its range or selected weekdays."):(periods,null);
    }
    private static async Task<List<PlannedPeriod>> FindConflicts(ErpDbContext db,SchedulingContext prepared,Guid faculty,List<PlannedPeriod> periods)
    {
        var first=periods[0].StartsAt;var last=periods[^1].EndsAt;var subject=prepared.Subject;
        var booked=await db.ClassSessions.Where(x=>x.InstitutionId==subject.InstitutionId&&!x.IsCancelled&&x.StartsAt<last&&x.EndsAt>first&&(x.ClassroomId==prepared.Room.Id||x.FacultyId==faculty||(x.ProgrammeId==subject.ProgrammeId&&x.AcademicSessionId==subject.AcademicSessionId&&x.Semester==subject.Semester&&x.Section==prepared.Section))).Select(x=>new{x.StartsAt,x.EndsAt,x.ClassroomId,x.FacultyId,x.ProgrammeId,x.AcademicSessionId,x.Semester,x.Section}).ToListAsync();
        return periods.Select(p=>p with{Conflicts=booked.Where(b=>b.StartsAt<p.EndsAt&&b.EndsAt>p.StartsAt).SelectMany(b=>new[]{b.ClassroomId==prepared.Room.Id?"Classroom":null,b.FacultyId==faculty?"Faculty":null,b.ProgrammeId==subject.ProgrammeId&&b.AcademicSessionId==subject.AcademicSessionId&&b.Semester==subject.Semester&&b.Section==prepared.Section?"Section":null}).OfType<string>().Distinct().ToArray()}).ToList();
    }
    public static void MapRecurringTimetable(this RouteGroupBuilder group,WebApplication app)
    {
        var routes=group.MapGroup("/academic/series").RequireAuthorization("Manage");
        routes.MapPost("/preview",async(Guid institutionId,SeriesRequest r,ErpDbContext db)=>
        {
            var expanded=Expand(r);if(expanded.Periods==null)return Results.Problem(expanded.Error,statusCode:400);
            var prepared=await ClassScheduling.Prepare(db,institutionId,r.SubjectId,r.FacultyId,r.ClassroomId,r.Section);if(prepared.Value==null)return Results.Problem(prepared.Error,statusCode:400);
            var periods=await FindConflicts(db,prepared.Value,r.FacultyId,expanded.Periods);
            return Results.Ok(new{timeZone="Asia/Kolkata",rosterCount=prepared.Value.Roster.Count,periods,hasConflicts=periods.Any(p=>p.Conflicts.Length>0)});
        });
        routes.MapPost("",async(Guid institutionId,SeriesRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            var expanded=Expand(r);if(expanded.Periods==null)return Results.Problem(expanded.Error,statusCode:400);
            var prepared=await ClassScheduling.Prepare(db,institutionId,r.SubjectId,r.FacultyId,r.ClassroomId,r.Section);if(prepared.Value==null)return Results.Problem(prepared.Error,statusCode:400);
            var series=new TimetableSeries{InstitutionId=institutionId,Name=r.Name.Trim(),SubjectId=r.SubjectId,FacultyId=r.FacultyId,ClassroomId=r.ClassroomId,Section=prepared.Value.Section,FirstDate=r.FirstDate,LastDate=r.LastDate,StartTime=r.StartTime,EndTime=r.EndTime,DaysOfWeek=r.DaysOfWeek.Order().ToArray(),ExcludedDates=r.ExcludedDates.Order().ToArray(),CreatedBy=ctx.Actor(),IsDemo=app.Environment.IsDevelopment()};
            db.TimetableSeries.Add(series);
            foreach(var p in expanded.Periods)
            {
                var session=ClassScheduling.AddPeriod(db,prepared.Value,r.FacultyId,p.StartsAt,p.EndsAt,series.IsDemo,series.Id);
                db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="Class.Scheduled",Resource=session.Id.ToString(),Details=$"Series {series.Id}; roster {prepared.Value.Roster.Count}; {p.StartsAt:O}–{p.EndsAt:O}."});
            }
            db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="TimetableSeries.Created",Resource=series.Id.ToString(),Details=$"{expanded.Periods.Count} dated periods; IST; {r.ExcludedDates.Length} skipped dates."});
            // SaveChanges uses one transaction for the template, every period, rosters and audits.
            // PostgreSQL exclusions resolve overlaps even when preview becomes stale.
            try{await db.SaveChangesAsync();}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.ExclusionViolation}){return Results.Problem("A period conflicts with a room, teacher or section booking. No part of this series was saved. Preview again.",statusCode:409);}
            return Results.Created($"/api/v1/institutions/{institutionId}/academic/series/{series.Id}",new{series.Id,series.Version,periodCount=expanded.Periods.Count});
        });
        routes.MapGet("",async(Guid institutionId,int? page,ErpDbContext db)=>
        {
            var p=Math.Clamp(page??1,1,10000);var query=db.TimetableSeries.Where(x=>x.InstitutionId==institutionId);
            var items=await query.OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id).Skip((p-1)*20).Take(20).Select(x=>new{x.Id,x.Name,x.FirstDate,x.LastDate,x.StartTime,x.EndTime,x.DaysOfWeek,x.TimeZone,x.IsCancelled,x.Version,periodCount=db.ClassSessions.Count(c=>c.InstitutionId==institutionId&&c.TimetableSeriesId==x.Id),cancelledCount=db.ClassSessions.Count(c=>c.InstitutionId==institutionId&&c.TimetableSeriesId==x.Id&&c.IsCancelled)}).ToListAsync();
            return Results.Ok(new{items,total=await query.CountAsync(),page=p,pageSize=20});
        });
        routes.MapGet("/{id:guid}",async(Guid institutionId,Guid id,ErpDbContext db)=>
        {
            var series=await db.TimetableSeries.SingleOrDefaultAsync(x=>x.InstitutionId==institutionId&&x.Id==id);if(series==null)return Results.NotFound();
            return Results.Ok(new{series,periods=await db.ClassSessions.Where(x=>x.InstitutionId==institutionId&&x.TimetableSeriesId==id).OrderBy(x=>x.StartsAt).Select(x=>new{x.Id,x.StartsAt,x.EndsAt,x.Version,x.IsCancelled,x.AttendanceSubmitted,rosterCount=db.ClassRosters.Count(c=>c.InstitutionId==institutionId&&c.ClassSessionId==x.Id)}).ToListAsync()});
        });
        routes.MapPost("/{id:guid}/cancel",async(Guid institutionId,Guid id,ArchiveRequest r,HttpContext ctx,ErpDbContext db)=>
        {
            if(string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Trim().Length<3||r.Reason.Length>500)return Results.Problem("Enter a cancellation reason of 3–500 characters.",statusCode:400);
            await using var tx=await db.Database.BeginTransactionAsync();
            var series=(await db.TimetableSeries.FromSqlInterpolated($"SELECT * FROM \"TimetableSeries\" WHERE \"InstitutionId\"={institutionId} AND \"Id\"={id} FOR UPDATE").ToListAsync()).SingleOrDefault();
            if(series==null)return Results.NotFound();if(series.Version!=r.Version||series.IsCancelled)return Results.Problem("Series changed or already cancelled. Reload before continuing.",statusCode:409);
            var classes=await db.ClassSessions.FromSqlInterpolated($"SELECT * FROM \"ClassSessions\" WHERE \"InstitutionId\"={institutionId} AND \"TimetableSeriesId\"={id} ORDER BY \"Id\" FOR UPDATE").ToListAsync();
            var future=classes.Where(x=>x.StartsAt>DateTime.UtcNow&&!x.IsCancelled&&!x.AttendanceSubmitted).ToList();
            if(future.Count==0)return Results.Problem("There are no remaining future periods to cancel.",statusCode:409);
            foreach(var c in future){c.IsCancelled=true;c.Version++;db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="Class.Cancelled",Resource=c.Id.ToString(),Details=r.Reason.Trim()});}
            series.IsCancelled=true;series.Version++;
            db.AuditLogs.Add(new AuditLog{InstitutionId=institutionId,ActorId=ctx.Actor(),Action="TimetableSeries.Cancelled",Resource=id.ToString(),Details=$"Cancelled {future.Count} future periods. {r.Reason.Trim()}"});
            await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{cancelled=future.Count,series.Version});
        });
    }
}
