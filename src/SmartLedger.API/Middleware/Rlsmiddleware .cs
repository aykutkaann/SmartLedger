using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartLedger.Infrastructure.Persistence;

namespace SmartLedger.API.Middleware;


public class RlsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var role = ctx.User.FindFirstValue(ClaimTypes.Role) ?? "User";

            // These raw SQL calls set session-level variables that PostgreSQL
            // RLS policies read via current_setting('app.current_user_id').
            await db.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.current_user_id', {0}, true)", userId);
            await db.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.current_role', {0}, true)", role);
        }

        await next(ctx);
    }
}