using Microsoft.AspNetCore.Mvc;
using superScrape.Models;
using superScrape.Services;
using System.Security.Cryptography;
using System.Text;

namespace superScrape.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperApiController : ControllerBase
{
    private readonly IGoogleMapsScraperService _scraperService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<ScraperApiController> _logger;

    public ScraperApiController(
        IGoogleMapsScraperService scraperService, 
        IBackgroundJobService backgroundJobService,
        ILogger<ScraperApiController> logger)
    {
        _scraperService = scraperService;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    [HttpPost("scrape")]
    public ActionResult<JobResponse> Scrape([FromBody] ScrapingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        if (request.MaxResults <= 0 || request.MaxResults > 200)
        {
            request.MaxResults = 20;
        }

        try
        {
            var clientId = GenerateClientId();
            
            _logger.LogInformation("API: Enqueueing scraping job for query: {Query}, MaxResults: {MaxResults}, ClientId: {ClientId}",
                request.Query, request.MaxResults, clientId);

            // Enqueue the job with client ID
            string jobId = _backgroundJobService.EnqueueScrapingJob(request.Query, request.MaxResults, clientId);

            return Ok(new JobResponse 
            { 
                JobId = jobId,
                Status = "queued",
                Message = "Your scraping job has been queued. Use the /job/{jobId} endpoint to check status."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error during job creation");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpGet("job/{jobId}")]
    public async Task<ActionResult<ScrapingResult>> GetJobResult(string jobId)
    {
        var result = await _backgroundJobService.GetJobResultAsync(jobId);
        
        if (result == null)
        {
            return NotFound(new { error = "Job not found" });
        }
        
        return Ok(result);
    }

    private string GenerateClientId()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var sessionId = HttpContext.Session?.Id ?? Guid.NewGuid().ToString();
        var combined = $"{ip}_{userAgent}_{sessionId}";
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash)[..8]; // First 8 characters
    }
}