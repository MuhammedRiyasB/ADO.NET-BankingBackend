using Banking.API.Models.Transaction;
using Banking.Application.DTOs.Transaction;
using Banking.Application.Services.Transactions;
using Banking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly IDepositService _depositService;
    private readonly IWithdrawService _withdrawService;
    private readonly ITransferService _transferService;

    public TransactionsController(
        IDepositService depositService,
        IWithdrawService withdrawService,
        ITransferService transferService)
    {
        _depositService = depositService;
        _withdrawService = withdrawService;
        _transferService = transferService;
    }

    [HttpPost("deposit")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Teller)}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Deposit([FromBody] DepositHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await _depositService.DepositAsync(
            new DepositRequest(request.AccountId, request.Amount, request.Narrative, request.Reference),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("withdraw")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Teller)}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Withdraw([FromBody] WithdrawHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await _withdrawService.WithdrawAsync(
            new WithdrawRequest(request.AccountId, request.Amount, request.Narrative, request.Reference),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("transfer")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Teller)}")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Transfer([FromBody] TransferHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await _transferService.TransferAsync(
            new TransferRequest(
                request.FromAccountId,
                request.ToAccountId,
                request.Amount,
                request.Narrative,
                request.ExternalReference),
            cancellationToken);

        return this.ToActionResult(result);
    }
}
