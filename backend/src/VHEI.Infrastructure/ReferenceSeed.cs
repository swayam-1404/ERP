using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VHEI.Domain;
namespace VHEI.Infrastructure;

public static class ReferenceSeed
{
    public static async Task RunAsync(ErpDbContext db, string file, bool demo, UserManager<ErpUser> users, RoleManager<IdentityRole<Guid>> roles, string? password)
    {
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(file));
        foreach (var item in doc.RootElement.GetProperty("institutions").EnumerateArray())
        {
            var code = item.GetProperty("code").GetString()!;
            var institution = await db.Institutions.SingleOrDefaultAsync(x => x.Code == code);
            if (institution == null)
            {
                institution = new Institution { Code = code, Name = item.GetProperty("name").GetProperty("value").GetString()!, Website = item.GetProperty("website").GetProperty("value").GetString(), SourceUrl = item.GetProperty("name").GetProperty("source_url").GetString()!, ExtractedOn = DateOnly.Parse("2026-10-14") };
                if (item.TryGetProperty("contact", out var contact)) institution.Address = contact.GetProperty("address").GetProperty("value").GetString();
                db.Institutions.Add(institution);
                await db.SaveChangesAsync();
            }
            if (item.TryGetProperty("departments", out var departments)) foreach(var department in departments.EnumerateArray())
            {
                var name = department.GetProperty("value").GetString()!;
                if (!await db.Departments.AnyAsync(x => x.InstitutionId == institution.Id && x.Name == name)) db.Departments.Add(new Department { InstitutionId = institution.Id, Name = name, SourceUrl = department.GetProperty("source_url").GetString()! });
            }
            await db.SaveChangesAsync();
            foreach(var programme in item.GetProperty("programmes").EnumerateArray())
            {
                var name = programme.GetProperty("name").GetProperty("value").GetString()!;
                if (await db.Programmes.AnyAsync(x => x.InstitutionId == institution.Id && x.Name == name)) continue;
                db.Programmes.Add(new Programme { InstitutionId = institution.Id, Name = name, DurationYears = programme.GetProperty("duration_years").ValueKind == JsonValueKind.Number ? programme.GetProperty("duration_years").GetInt32() : null, SourceUrl = programme.GetProperty("name").GetProperty("source_url").GetString()! });
            }
            if (demo && !await db.AcademicSessions.AnyAsync(x => x.InstitutionId == institution.Id)) db.AcademicSessions.Add(new AcademicSession { InstitutionId = institution.Id, Name = "2026–27", IsDemo = true });
            await db.SaveChangesAsync();
        }
        if (!demo) return;
        if (string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Seed__AdminPassword is required for development account creation.");
        foreach(var role in new[] { "CentralAdministrator", "InstitutionAdministrator", "Faculty" }) if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole<Guid>(role));
        var all = await db.Institutions.ToListAsync();
        foreach(var entry in new[] { ("admin@example.test", "Demo Central Administrator", "CentralAdministrator"), ("institution@example.test", "Demo VIT Administrator", "InstitutionAdministrator"), ("faculty@example.test", "Demo Faculty Member", "Faculty") })
        {
            var user = await users.FindByEmailAsync(entry.Item1);
            if (user == null)
            {
                user = new ErpUser { Id = Guid.NewGuid(), UserName = entry.Item1, Email = entry.Item1, EmailConfirmed = true, DisplayName = entry.Item2 };
                var result = await users.CreateAsync(user, password);
                if (!result.Succeeded) throw new InvalidOperationException("Development user seed rejected: " + string.Join(", ",result.Errors.Select(e => e.Code)));
                await users.AddToRoleAsync(user, entry.Item3);
            }
            foreach(var inst in all.Where(i => entry.Item3 == "CentralAdministrator" || i.Code == "VIT")) if (!await db.InstitutionAssignments.AnyAsync(x => x.UserId == user.Id && x.InstitutionId == inst.Id)) db.InstitutionAssignments.Add(new InstitutionAssignment { UserId = user.Id, InstitutionId = inst.Id });
            await db.SaveChangesAsync();
        }
    }
}
