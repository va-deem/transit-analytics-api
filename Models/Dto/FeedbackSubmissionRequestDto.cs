using System.ComponentModel.DataAnnotations;

namespace TransitAnalyticsAPI.Models.Dto;

public class FeedbackSubmissionRequestDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}
