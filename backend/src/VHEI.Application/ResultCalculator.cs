using VHEI.Domain;
namespace VHEI.Application;
public record CalculatedMark(decimal Total,decimal Percentage,bool IsPass,string Grade,decimal GradePoints);
public static class ResultCalculator
{
    // Percentage and GPA use two decimals, midpoint away from zero. The policy
    // approval UI documents this algorithm; no official band defaults are seeded.
    public static CalculatedMark Calculate(ExamAssessment exam,ExamMark mark,IReadOnlyList<GradeBand> bands,string failGrade)
    {
        var total=mark.Attendance=="Present"?(mark.InternalMark??0)+(mark.ExternalMark??0):0;
        var percentage=decimal.Round(100m*total/(exam.MaximumInternal+exam.MaximumExternal),2,MidpointRounding.AwayFromZero);
        var pass=mark.Attendance=="Present"&&mark.InternalMark>=exam.PassInternal&&mark.ExternalMark>=exam.PassExternal&&total>=exam.PassTotal;
        var band=bands.OrderByDescending(x=>x.MinimumPercent).First(x=>x.MinimumPercent<=percentage);
        return new(total,percentage,pass,pass?band.Grade:failGrade,pass?band.Points:0m);
    }
    public static decimal Gpa(IEnumerable<(decimal Credits,decimal Points)> items)
    {
        var rows=items.ToList();var credits=rows.Sum(x=>x.Credits);return credits==0?0:decimal.Round(rows.Sum(x=>x.Credits*x.Points)/credits,2,MidpointRounding.AwayFromZero);
    }
}
