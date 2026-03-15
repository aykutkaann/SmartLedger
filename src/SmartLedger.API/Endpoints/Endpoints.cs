using System.Security.Claims;
using MediatR;
using SmartLedger.Application.Accounts;
using SmartLedger.Application.Auth;
using SmartLedger.Application.Transfers;

namespace SmartLedger.API.Endpoints;

public static class AuthEndpointExtensions
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterCommand cmd, IMediator med) =>
        {
            var result = await med.Send(cmd);
            return Results.Ok(result);
        });

        group.MapPost("/login", async (LoginCommand cmd, IMediator med) =>
        {
            var result = await med.Send(cmd);
            return Results.Ok(result);
        });

        group.MapPost("/refresh", async (RefreshCommand cmd, IMediator med) =>
        {
            var result = await med.Send(cmd);
            return Results.Ok(result);
        });
    }
}

public static class AccountEndpointExtensions
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts").WithTags("Accounts").RequireAuthorization();

        // GET /accounts — list my accounts
        group.MapGet("/", async (ClaimsPrincipal user, IMediator med) =>
        {
            var userId = GetUserId(user);
            var result = await med.Send(new GetMyAccountQuery(userId));
            return Results.Ok(result);
        });

        // POST /accounts — open new account
        group.MapPost("/", async (CreateAccountRequest req, ClaimsPrincipal user, IMediator med) =>
        {
            var userId = GetUserId(user);
            var result = await med.Send(new CreateAccountCommand(userId, req.Currency));
            return Results.Created($"/accounts/{result.Id}", result);
        });

        // GET /accounts/{id}/transactions — paginated history
        group.MapGet("/{id:guid}/transactions", async (
            Guid id, ClaimsPrincipal user, IMediator med,
            int page = 1, int pageSize = 20) =>
        {
            var userId = GetUserId(user);
            var result = await med.Send(new GetTransactionHistoryQuery(id, userId, page, pageSize));
            return Results.Ok(result);
        });

        // DELETE /accounts/{id} — close account
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, IMediator med) =>
        {
            var userId = GetUserId(user);
            await med.Send(new CloseAccountCommand(id, userId));
            return Results.NoContent();
        });
    }

    private static Guid GetUserId(ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public static class TransferEndpointExtensions
{
    public static void MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/transfers").WithTags("Transfers").RequireAuthorization();

        // POST /transfers — initiate transfer
        group.MapPost("/", async (
            TransferRequest req, ClaimsPrincipal user,
            IMediator med, HttpContext ctx) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ip = ctx.Connection.RemoteIpAddress?.ToString();

            var result = await med.Send(new InitiateTransferCommand(
                userId, req.FromAccountId, req.ToAccountId,
                req.Amount, req.Description, ip));

            return Results.Created($"/transfers/{result.Id}", result);
        });

        // GET /transfers/{id} — get transfer with fraud signals
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IMediator med) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await med.Send(new GetTransferQuery(id, userId));
            return Results.Ok(result);
        });

        // GET /transfers/admin/fraud-queue — admin only
        app.MapGet("/admin/fraud-queue", async (IMediator med) =>
        {
            // TODO: wire up GetFlaggedTransactionsQuery
            return Results.Ok(new { message = "Fraud queue endpoint — Admin only" });
        }).RequireAuthorization("Admin").WithTags("Admin");
    }
}

// ── Request models ────────────────────────────────────────────────────────────
public record CreateAccountRequest(string Currency);