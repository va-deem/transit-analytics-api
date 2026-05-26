using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Dto;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Controllers;

[ApiController]
[Route("feedback")]
public class FeedbackController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public FeedbackController(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    [HttpPost]
    [EnableRateLimiting("feedback-submissions")]
    public async Task<IActionResult> Create(
        [FromBody] FeedbackSubmissionRequestDto request,
        CancellationToken cancellationToken)
    {
        string name = request.Name.Trim();
        string email = request.Email.Trim();
        string message = request.Message.Trim();
        string ip = ResolveIp();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                error = "invalid_name",
                message = "A name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                error = "invalid_email",
                message = "An email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(new
            {
                error = "invalid_message",
                message = "A message is required."
            });
        }

        var submission = new FeedbackSubmission
        {
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Name = name,
            Email = email,
            Message = message,
            Ip = ip
        };

        _dbContext.FeedbackSubmissions.Add(submission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = submission.Id }, new
        {
            id = submission.Id,
            createdAtUtc = submission.CreatedAtUtc
        });
    }

    [HttpGet("{id:long}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var submission = await _dbContext.FeedbackSubmissions
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.CreatedAtUtc,
                item.Name,
                item.Email,
                item.Message,
                item.Ip
            })
            .SingleOrDefaultAsync(cancellationToken);

        return submission is null ? NotFound() : Ok(submission);
    }

    private string ResolveIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
