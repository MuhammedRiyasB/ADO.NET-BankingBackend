using Banking.API.Models.Customer;
using Banking.Application.DTOs.Customer;
using Banking.Application.Interfaces;
using Banking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Teller)}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Create([FromBody] CreateCustomerHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.CreateAsync(
            new CreateCustomerRequest(request.FullName, request.Email, request.PhoneNumber),
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return this.ToErrorResult(result.ErrorCode, result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetById), new { customerId = result.Value.Id }, result.Value);
    }

    [HttpGet("{customerId:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Teller)},{nameof(UserRole.Auditor)}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetByIdAsync(customerId, cancellationToken);
        return this.ToActionResult(result);
    }
}
