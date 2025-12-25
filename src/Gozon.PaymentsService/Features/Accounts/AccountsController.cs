using Microsoft.AspNetCore.Mvc;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Features.Accounts.Contracts;

namespace PaymentsService.Features.Accounts;

[ApiController]
[Route("api/accounts")]
public class AccountsController(IAccountsService accountsService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BalanceResponse>> Create([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var existing = await accountsService.GetBalanceByUserAsync(request.UserId, cancellationToken);
        if (existing is not null)
        {
            return Conflict(existing);
        }

        var account = await accountsService.CreateAsync(request, cancellationToken);
        var balance = await accountsService.GetBalanceAsync(account.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, balance);
    }

    [HttpPost("{id:guid}/topup")]
    public async Task<ActionResult<BalanceResponse>> TopUp([FromRoute] Guid id, [FromBody] TopUpRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Top up amount must be positive");
        }

        var balance = await accountsService.TopUpAsync(id, request, cancellationToken);
        if (balance is null)
        {
            return NotFound();
        }

        return balance;
    }

    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<BalanceResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var balance = await accountsService.GetBalanceAsync(id, cancellationToken);
        if (balance is null)
        {
            return NotFound();
        }

        return balance;
    }

    [HttpGet("by-user/{userId:guid}/balance")]
    public async Task<ActionResult<BalanceResponse>> GetByUser([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var balance = await accountsService.GetBalanceByUserAsync(userId, cancellationToken);
        if (balance is null)
        {
            return NotFound();
        }

        return balance;
    }
}

