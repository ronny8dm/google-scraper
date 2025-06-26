using Microsoft.AspNetCore.Mvc;
using superScrape.Models;
using superScrape.Services;

namespace superScrape.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperApiController : ControllerBase
{
    private readonly IGoogleMapsScraperService _scraperService;
    private readonly ILogger<ScraperApiController> _logger;

    public ScraperApiController(IGoogleMapsScraperService scraperService, ILogger<ScraperApiController> logger)
    {
        _scraperService = scraperService;
        _logger = logger;
    }

    [HttpPost("scrape")]
    public async Task<ActionResult<ScrapingResult>> Scrape([FromBody] ScrapingRequest request)
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
            _logger.LogInformation("API: Starting scraping for query: {Query}, MaxResults: {MaxResults}",
                request.Query, request.MaxResults);

            var result = await _scraperService.ScrapBusinessListingsAsync(request.Query, request.MaxResults);

            _logger.LogInformation("API: Scraping completed. Success: {Success}, Found: {Count} businesses",
                result.Success, result.Businesses.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error during scraping process");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}