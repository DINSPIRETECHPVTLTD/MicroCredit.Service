using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.PaymentTerm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class PaymentTermController : ControllerBase
{
    private readonly IPaymentTermService _service;
    private readonly ILogger<PaymentTermController> _logger;

    public PaymentTermController(IPaymentTermService service, ILogger<PaymentTermController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPaymentTerms(CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentTerms = await _service.GetAllPaymentTermsAsync(cancellationToken);
            return Ok(paymentTerms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment terms");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPaymentTermById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentTerm = await _service.GetPaymentTermByIdAsync(id, cancellationToken);
            if (paymentTerm == null)
                return NotFound($"Payment term with ID {id} not found.");

            return Ok(paymentTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment term with ID {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentTerm([FromBody] CreatePaymentTermRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.PaymentTermName) || string.IsNullOrWhiteSpace(request.PaymentType))
                return BadRequest("Payment term name and payment type are required.");

            var userId = UserClaimsHelper.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var id = await _service.CreatePaymentTermAsync(request, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetPaymentTermById), new { id }, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment term");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePaymentTerm(int id, [FromBody] UpdatePaymentTermRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.PaymentTermName) || string.IsNullOrWhiteSpace(request.PaymentType))
                return BadRequest("Payment term name and payment type are required.");

            var userId = UserClaimsHelper.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var result = await _service.UpdatePaymentTermAsync(id, request, userId.Value, cancellationToken);
            if (!result)
                return NotFound($"Payment term with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment term with ID {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePaymentTerm(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.DeletePaymentTermAsync(id, cancellationToken);
            if (!result)
                return NotFound($"Payment term with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment term with ID {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}