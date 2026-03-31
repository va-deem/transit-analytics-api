using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Services;

/// <summary>
/// Persists application-level log entries to the database.
/// The source component name is derived automatically from <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The component class that is producing the log entries.</typeparam>
/// <example>
/// <code>
/// // Inject into your class:
/// private readonly ISystemLogService&lt;MyService&gt; _systemLog;
///
/// // Log an informational message:
/// await _systemLog.LogAsync(SystemLogType.Info, "Deleted 42 expired positions");
///
/// // Log an error with stack trace:
/// catch (Exception ex)
/// {
///     await _systemLog.LogAsync(SystemLogType.Error, "Cleanup failed", ex.ToString());
/// }
/// </code>
/// </example>
public interface ISystemLogService<T>
{
    /// <summary>
    /// Writes a log entry to the SystemLogs table.
    /// </summary>
    /// <param name="type">The severity level of the log entry.</param>
    /// <param name="description">A short summary of what happened.</param>
    /// <param name="details">Optional extended information such as a stack trace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync(SystemLogType type, string description, string? details = null,
        CancellationToken cancellationToken = default);
}