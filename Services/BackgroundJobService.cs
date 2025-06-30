using superScrape.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace superScrape.Services;

public interface IBackgroundJobService
{
    string EnqueueScrapingJob(string query, int maxResults, string? clientId = null);
    Task<ScrapingResult?> GetJobResultAsync(string jobId);
}

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IScraperServiceFactory _scraperServiceFactory;
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly ConcurrentDictionary<string, ScrapingResult> _jobResults = new();
    private readonly ConcurrentDictionary<string, (string status, DateTime createdAt, string clientId)> _jobStatus = new();
    private readonly SemaphoreSlim _concurrencyLimit;
    private readonly int _maxConcurrentJobs = 4; // Allow up to 4 concurrent scraping jobs

    public BackgroundJobService(IScraperServiceFactory scraperServiceFactory, ILogger<BackgroundJobService> logger)
    {
        _scraperServiceFactory = scraperServiceFactory;
        _logger = logger;
        _concurrencyLimit = new SemaphoreSlim(_maxConcurrentJobs, _maxConcurrentJobs);

        // Start cleanup task
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
                CleanupOldJobs();
            }
        });
    }

    public string EnqueueScrapingJob(string query, int maxResults, string? clientId = null)
    {
        string jobId = Guid.NewGuid().ToString();
        clientId ??= $"client_{DateTime.UtcNow.Ticks % 10000}";
        
        _jobStatus[jobId] = ("queued", DateTime.UtcNow, clientId);

        // Start the job immediately (don't use ActionBlock queue)
        _ = Task.Run(async () => await ProcessScrapingJobAsync(jobId, query, maxResults));

        _logger.LogInformation("Job {JobId} queued for client {ClientId}", jobId, clientId);
        return jobId;
    }

    private async Task ProcessScrapingJobAsync(string jobId, string query, int maxResults)
    {
        // Wait for available slot
        await _concurrencyLimit.WaitAsync();

        try
        {
            if (_jobStatus.TryGetValue(jobId, out var statusInfo))
            {
                _jobStatus[jobId] = ("processing", statusInfo.createdAt, statusInfo.clientId);
            }

            _logger.LogInformation("Starting job {JobId} for query: {Query} (Available slots: {Available})", 
                jobId, query, _concurrencyLimit.CurrentCount);
            
            using var scope = _scraperServiceFactory.CreateScope();
            var scraperService = scope.ServiceProvider.GetRequiredService<IGoogleMapsScraperService>();
            
            var result = await scraperService.ScrapBusinessListingsAsync(query, maxResults);
            
            _jobResults[jobId] = result;
            if (_jobStatus.TryGetValue(jobId, out var currentStatus))
            {
                _jobStatus[jobId] = ("completed", currentStatus.createdAt, currentStatus.clientId);
            }
            
            _logger.LogInformation("Job {JobId} completed. Found {Count} results", 
                jobId, result.Businesses?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
            
            if (_jobStatus.TryGetValue(jobId, out var errorStatus))
            {
                _jobStatus[jobId] = ("failed", errorStatus.createdAt, errorStatus.clientId);
            }
            
            _jobResults[jobId] = new ScrapingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Query = query,
                ScrapedAt = DateTime.UtcNow
            };
        }
        finally
        {
            _concurrencyLimit.Release();
        }
    }

    public Task<ScrapingResult?> GetJobResultAsync(string jobId)
    {
        if (_jobResults.TryGetValue(jobId, out var result))
        {
            return Task.FromResult<ScrapingResult?>(result);
        }
        
        if (_jobStatus.TryGetValue(jobId, out var statusInfo))
        {
            var processingCount = _jobStatus.Values.Count(s => s.status == "processing");
            var queuedCount = _jobStatus.Values.Count(s => s.status == "queued");
            
            string statusMessage = statusInfo.status switch
            {
                "queued" => $"Job is queued. {processingCount} jobs processing, {queuedCount} queued",
                "processing" => "Job is currently being processed",
                "failed" => "Job has failed",
                "rejected" => "Job was rejected due to system capacity",
                _ => $"Job status: {statusInfo.status}"
            };

            return Task.FromResult<ScrapingResult?>(new ScrapingResult 
            { 
                Success = false,
                ErrorMessage = statusMessage,
                Query = "Processing",
                ScrapedAt = DateTime.UtcNow
            });
        }
        
        return Task.FromResult<ScrapingResult?>(new ScrapingResult
        {
            Success = false,
            ErrorMessage = "Job not found",
            Query = "Not Found",
            ScrapedAt = DateTime.UtcNow
        });
    }

    private void CleanupOldJobs()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-2);
        var jobsToRemove = _jobStatus.Where(kvp => 
            kvp.Value.createdAt < cutoffTime && 
            kvp.Value.status != "processing"
        ).Select(kvp => kvp.Key).ToList();

        foreach (var jobId in jobsToRemove)
        {
            _jobResults.TryRemove(jobId, out _);
            _jobStatus.TryRemove(jobId, out _);
        }

        if (jobsToRemove.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old jobs", jobsToRemove.Count);
        }
    }
}