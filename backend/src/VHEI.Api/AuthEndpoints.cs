using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VHEI.Infrastructure;
namespace VHEI.Api;

public record LoginRequest(string Email, string Password, string? Code);
public record MfaRequest(string Code);
public record PasswordChangeRequest(string CurrentPassword,string NewPassword);
public static class AuthEndpoints
{
    public static void MapAuth(this WebApplication app)
    {
        var auth=app.MapGroup("/api/v1/auth");
        auth.MapGet("/csrf", (IAntiforgery antiforgery, HttpContext ctx) => Results.Ok(new { token = antiforgery.GetAndStoreTokens(ctx).RequestToken }));
        auth.MapPost("/login", async (LoginRequest request, SignInManager<ErpUser> signIn, UserManager<ErpUser> users) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 || string.IsNullOrEmpty(request.Password) || request.Password.Length > 128) return Results.ValidationProblem(new Dictionary<string,string[]> { ["login"] = ["Enter a valid email and password."] });
            var user = await users.FindByEmailAsync(request.Email.Trim());
            if (user == null) return Results.Problem("Email or password is incorrect.",statusCode:401);
            var result=await signIn.PasswordSignInAsync(user,request.Password,false,true);
            if (result.RequiresTwoFactor) return Results.Ok(new { requiresMfa = true });
            return result.Succeeded ? Results.Ok(new { requiresMfa = false }) : Results.Problem(result.IsLockedOut ? "Account temporarily locked. Try again later." : "Email or password is incorrect.",statusCode:401);
        }).RequireRateLimiting("login");
        auth.MapPost("/mfa/login", async (MfaRequest request, SignInManager<ErpUser> signIn) =>
        {
            var user=await signIn.GetTwoFactorAuthenticationUserAsync();
            if(user==null)return Results.Problem("Start password sign-in first.",statusCode:401);
            var result=await signIn.TwoFactorAuthenticatorSignInAsync(request.Code.Replace(" ",""),false,false);
            if(result.Succeeded)await signIn.SignInWithClaimsAsync(user,false,[new System.Security.Claims.Claim("amr","mfa")]);
            return result.Succeeded ? Results.Ok(new { requiresMfa = false }) : Results.Problem("Authenticator code rejected.",statusCode:401);
        }).RequireRateLimiting("login");
        auth.MapPost("/logout", async(SignInManager<ErpUser> signIn) => { await signIn.SignOutAsync(); return Results.NoContent(); }).RequireAuthorization();
        auth.MapGet("/me", async(HttpContext ctx, UserManager<ErpUser> users, ErpDbContext db) =>
        {
            var user = await users.GetUserAsync(ctx.User);
            if (user == null) return Results.Unauthorized();
            var roles=await users.GetRolesAsync(user);
            var institutions=await (from assignment in db.InstitutionAssignments join institution in db.Institutions on assignment.InstitutionId equals institution.Id where assignment.UserId==user.Id orderby institution.Code select new { institution.Id,institution.Code,institution.Name }).ToListAsync();
            return Results.Ok(new { user.Id,user.Email,user.DisplayName,roles,institutions,mfaEnabled=user.TwoFactorEnabled,user.MustChangePassword });
        }).RequireAuthorization();
        auth.MapPost("/password/change",async(PasswordChangeRequest r,HttpContext ctx,UserManager<ErpUser> users,SignInManager<ErpUser> signIn)=>
        {
            if(string.IsNullOrEmpty(r.CurrentPassword)||r.CurrentPassword.Length>128||string.IsNullOrEmpty(r.NewPassword)||r.NewPassword.Length is <12 or >128||r.NewPassword==r.CurrentPassword)return Results.Problem("Provide the current password and a different new password of 12–128 characters.",statusCode:400);
            var user=await users.GetUserAsync(ctx.User);if(user==null)return Results.Unauthorized();
            var result=await users.ChangePasswordAsync(user,r.CurrentPassword,r.NewPassword);if(!result.Succeeded)return Results.Problem("Password change rejected. Check the current password and use a strong new password.",statusCode:400);
            user.MustChangePassword=false;await users.UpdateAsync(user);await signIn.SignOutAsync();return Results.Ok(new{signInAgain=true});
        }).RequireAuthorization().RequireRateLimiting("login");
        auth.MapGet("/mfa/setup", async(HttpContext ctx, UserManager<ErpUser> users) =>
        {
            var user=(await users.GetUserAsync(ctx.User))!;
            var key=await users.GetAuthenticatorKeyAsync(user);
            return Results.Ok(new { key, enabled=user.TwoFactorEnabled });
        }).RequireAuthorization();
        auth.MapPost("/mfa/setup", async(HttpContext ctx, UserManager<ErpUser> users) =>
        {
            var user=(await users.GetUserAsync(ctx.User))!;
            if (user.TwoFactorEnabled) return Results.Problem("MFA is already enabled.",statusCode:409);
            await users.ResetAuthenticatorKeyAsync(user);
            return Results.Ok(new { key=await users.GetAuthenticatorKeyAsync(user) });
        }).RequireAuthorization();
        auth.MapPost("/mfa/enable", async(MfaRequest request, HttpContext ctx, UserManager<ErpUser> users, SignInManager<ErpUser> signIn) =>
        {
            var user=(await users.GetUserAsync(ctx.User))!;
            if (!await users.VerifyTwoFactorTokenAsync(user,users.Options.Tokens.AuthenticatorTokenProvider,request.Code.Replace(" ",""))) return Results.Problem("Authenticator code rejected.",statusCode:400);
            await users.SetTwoFactorEnabledAsync(user,true);
            await signIn.SignOutAsync();
            return Results.Ok(new { enabled=true, signInAgain=true });
        }).RequireAuthorization();
    }
}
