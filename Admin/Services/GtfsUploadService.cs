using System.IO.Compression;
using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Admin.Services;

public class GtfsUploadService : IGtfsUploadService
{
    private static readonly string[] RequiredFiles =
    [
        "feed_info.txt",
        "routes.txt",
        "trips.txt",
        "stops.txt",
        "stop_times.txt",
        "shapes.txt"
    ];

    private readonly IGtfsImportService _gtfsImportService;

    public GtfsUploadService(IGtfsImportService gtfsImportService)
    {
        _gtfsImportService = gtfsImportService;
    }

    public async Task<GtfsUploadJob> SaveUploadAsync(IFormFile archive, CancellationToken cancellationToken = default)
    {
        ValidateArchive(archive);

        var workRoot = Path.Combine(Path.GetTempPath(), "transit-analytics-gtfs", Guid.NewGuid().ToString("N"));
        var zipPath = Path.Combine(workRoot, archive.FileName);

        Directory.CreateDirectory(workRoot);

        await using (var output = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write))
        {
            await archive.CopyToAsync(output, cancellationToken);
        }

        return new GtfsUploadJob
        {
            FileName = archive.FileName,
            WorkRoot = workRoot
        };
    }

    public async Task<GtfsUploadResult> ImportFromWorkRootAsync(
        string workRoot,
        Func<string, CancellationToken, Task>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        var zipPaths = Directory.GetFiles(workRoot, "*.zip", SearchOption.TopDirectoryOnly);
        if (zipPaths.Length != 1)
        {
            throw new InvalidOperationException("Expected exactly one GTFS archive in the upload workspace.");
        }

        var zipPath = zipPaths[0];
        var extractPath = Path.Combine(workRoot, "extracted");
        Directory.CreateDirectory(extractPath);

        try
        {
            if (reportProgress is not null)
            {
                await reportProgress("extracting", cancellationToken);
            }

            ExtractArchiveSafely(zipPath, extractPath);

            if (reportProgress is not null)
            {
                await reportProgress("validating", cancellationToken);
            }

            ValidateRequiredFiles(extractPath);

            if (reportProgress is not null)
            {
                await reportProgress("importing", cancellationToken);
            }

            var importResult = await _gtfsImportService.ImportRoutesAndTripsAsync(extractPath, cancellationToken);

            return new GtfsUploadResult
            {
                SourceVersion = importResult.SourceVersion,
                ImportResult = importResult
            };
        }
        finally
        {
            if (Directory.Exists(workRoot))
            {
                Directory.Delete(workRoot, recursive: true);
            }
        }
    }

    private static void ValidateArchive(IFormFile archive)
    {
        if (archive.Length == 0)
        {
            throw new InvalidOperationException("The uploaded GTFS archive is empty.");
        }

        if (!string.Equals(Path.GetExtension(archive.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("GTFS upload must be a .zip archive.");
        }
    }

    private static void ExtractArchiveSafely(string zipPath, string destinationDirectory)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName))
            {
                continue;
            }

            var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
            if (!destinationPath.StartsWith(Path.GetFullPath(destinationDirectory), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The uploaded archive contains an invalid entry path.");
            }

            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }

    private static void ValidateRequiredFiles(string extractPath)
    {
        var availableFiles = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(fileName => fileName!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingFiles = RequiredFiles
            .Where(requiredFile => !availableFiles.Contains(requiredFile))
            .ToList();

        if (missingFiles.Count > 0)
        {
            throw new InvalidOperationException($"GTFS archive is missing required files: {string.Join(", ", missingFiles)}.");
        }
    }
}
