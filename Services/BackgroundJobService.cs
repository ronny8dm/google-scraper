using superScrape.Models;
using Microsoft.Extensions.DependencyInjection;

namespace superScrape.Services;

public interface IBackgroundJobService
{
    string EnqueueScrapingJob(string query, int maxResults);
    Task<ScrapingResult?> GetJobResultAsync(string jobId);
}

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IScraperServiceFactory _scraperServiceFactory;
    private readonly ILogger<BackgroundJobService> _logger;
    private static readonly Dictionary<string, ScrapingResult> _jobResults = new();
    private static readonly Dictionary<string, string> _jobStatus = new();

    public BackgroundJobService(IScraperServiceFactory scraperServiceFactory, ILogger<BackgroundJobService> logger)
    {
        _scraperServiceFactory = scraperServiceFactory;
        _logger = logger;
    }

    public string EnqueueScrapingJob(string query, int maxResults)
    {
        // Generate a unique job ID
        string jobId = Guid.NewGuid().ToString();
        
        // Store initial state
        _jobStatus[jobId] = "queued";
        
        // Start background task
        _ = ProcessScrapingJobAsync(jobId, query, maxResults);
        
        return jobId;
    }

    public Task<ScrapingResult?> GetJobResultAsync(string jobId)
    {
        if (_jobResults.TryGetValue(jobId, out var result))
        {
            return Task.FromResult<ScrapingResult?>(result);
        }
        
        if (_jobStatus.TryGetValue(jobId, out var status))
        {
            return Task.FromResult<ScrapingResult?>(new ScrapingResult 
            { 
                Success = false,
                ErrorMessage = $"Job is still {status}. Please try again later.",
                Query = "Processing"
            });
        }
        
        return Task.FromResult<ScrapingResult?>(null); // Job not found
    }
    
    private async Task ProcessScrapingJobAsync(string jobId, string query, int maxResults)
    {
        try
        {
            _jobStatus[jobId] = "processing";
            _logger.LogInformation("Starting background job {JobId} for query: {Query}", jobId, query);
            
            // Create a new scraper service for this job
            var scraperService = _scraperServiceFactory.CreateScraperService();
            
            var result = await scraperService.ScrapBusinessListingsAsync(query, maxResults);
            
            // Store the result
            _jobResults[jobId] = result;
            _jobStatus[jobId] = "completed";
            
            _logger.LogInformation("Background job {JobId} completed. Found {Count} results", 
                jobId, result.Businesses?.Count ?? 0);
                
            // Optional: Clean up old jobs periodically
            CleanupOldJobs();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing background job {JobId}", jobId);
            _jobStatus[jobId] = "failed";
            
            // Store error result
            _jobResults[jobId] = new ScrapingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Query = query,
                ScrapedAt = DateTime.UtcNow
            };
        }
    }
    
    private void CleanupOldJobs()
    {
        // Simple cleanup - run on a timer in production
        const int maxJobsToKeep = 100;
        
        if (_jobResults.Count > maxJobsToKeep)
        {
            var oldestJobs = _jobResults.Keys
                .Except(_jobStatus.Keys.Where(k => _jobStatus[k] == "processing"))
                .OrderBy(k => k)
                .Take(_jobResults.Count - maxJobsToKeep);
                
            foreach (var jobId in oldestJobs)
            {
                _jobResults.Remove(jobId);
                _jobStatus.Remove(jobId);
            }
        }
    }
}