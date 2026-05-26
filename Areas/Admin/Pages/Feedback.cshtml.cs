using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Areas.Admin.Pages;

[Authorize(Policy = "AdminOnly")]
public class FeedbackModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public FeedbackModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<FeedbackListItem> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _dbContext.FeedbackSubmissions
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new FeedbackListItem(
                item.Id,
                item.CreatedAtUtc,
                item.Name,
                item.Email,
                item.Message,
                item.Ip))
            .ToListAsync(cancellationToken);
    }

    public sealed record FeedbackListItem(
        long Id,
        DateTime CreatedAtUtc,
        string Name,
        string Email,
        string Message,
        string Ip);
}
