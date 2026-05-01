using Banking.API.Models.Account;
using Banking.Application.DTOs.Account;
using Banking.Application.Interfaces;
using Banking.Application.Services.Accounts;
using Banking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IStatementService _statementService;

    public AccountsController(IAccountService accountService, IStatementService statementService)
    {
        _accountService = accountService;
        _statementService = statementService;
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Teller)}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Open([FromBody] OpenAccountHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountService.OpenAccountAsync(
            new OpenAccountRequest(
                request.CustomerId,
                request.AccountType,
                request.Currency,
                request.DailyDebitLimit),
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return this.ToErrorResult(result.ErrorCode, result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetById), new { accountId = result.Value.Id }, result.Value);
    }

    [HttpGet("{accountId:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Teller)},{nameof(UserRole.Auditor)}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await _accountService.GetByIdAsync(accountId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{accountId:guid}/statement")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Teller)},{nameof(UserRole.Auditor)}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<StatementEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetStatement(
        Guid accountId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var result = await _statementService.GetStatementAsync(accountId, fromUtc, toUtc, cancellationToken);
        return this.ToActionResult(result);
    }
}
