using Microsoft.AspNetCore.Mvc;
using superScrape.Models;
using superScrape.Services;

namespace superScrape.Controllers;

public class ScraperController : Controller
{
    private readonly IGoogleMapsScraperService _scraperService;
    private readonly ILogger<ScraperController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ScraperController(IGoogleMapsScraperService scraperService, ILogger<ScraperController> logger, IWebHostEnvironment environment)
    {
        _scraperService = scraperService;
        _logger = logger;
        _environment = environment;
    }

    public IActionResult Index()
    {
        return View(new ScrapingRequest());
    }

    [HttpPost]
    public async Task<IActionResult> Scrape(ScrapingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            ModelState.AddModelError("Query", "Please enter a search query.");
            return View("Index", request);
        }

        if (request.MaxResults <= 0 || request.MaxResults > 100)
        {
            request.MaxResults = 20;
        }

        try
        {
            _logger.LogInformation("Starting scraping for query: {Query}, MaxResults: {MaxResults}",
                request.Query, request.MaxResults);

            var result = await _scraperService.ScrapBusinessListingsAsync(request.Query, request.MaxResults);

            if (result.Success)
            {
                _logger.LogInformation("Scraping completed successfully. Found {Count} businesses",
                    result.Businesses.Count);
                
                // Always redirect to results if scraping was successful, regardless of count
                return View("Results", result);
            }
            else
            {
                _logger.LogError("Scraping failed: {Error}", result.ErrorMessage);
                ModelState.AddModelError("", $"Scraping failed: {result.ErrorMessage}");
                return View("Index", request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scraping process");
            ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            return View("Index", request);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ScrapeApi(string query, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required" });
        }

        if (maxResults <= 0 || maxResults > 100)
        {
            maxResults = 20;
        }

        try
        {
            var result = await _scraperService.ScrapBusinessListingsAsync(query, maxResults);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API scraping process");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult Export(string query, List<BusinessListing> businesses)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query is required");
        }

        if (businesses == null || !businesses.Any())
        {
            TempData["Error"] = "No business data to export";
            return RedirectToAction("Index");
        }

        try
        {
            // Generate CSV content using the passed businesses data (no scraping)
            var csv = GenerateCsv(businesses);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            var fileName = $"google_maps_scrape_{query.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during export process");
            return StatusCode(500, ex.Message);
        }
    }

    private string GenerateCsv(List<BusinessListing> businesses)
    {
        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine("Name,Address,Phone,Website,Rating,ReviewCount,Category,Hours,GoogleMapsUrl,Description");

        // Data rows
        foreach (var business in businesses)
        {
            csv.AppendLine($"\"{EscapeCsv(business.Name)}\",\"{EscapeCsv(business.Address)}\",\"{EscapeCsv(business.Phone)}\",\"{EscapeCsv(business.Website)}\",{business.Rating},{business.ReviewCount},\"{EscapeCsv(business.Category)}\",\"{EscapeCsv(business.Hours)}\",\"{EscapeCsv(business.GoogleMapsUrl)}\",\"{EscapeCsv(business.Description)}\"");
        }

        return csv.ToString();
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\"", "\"\"");
    }

    [HttpGet]
    public IActionResult Screenshots()
    {
        var screenshotsPath = Path.Combine(_environment.WebRootPath, "screenshots");
        var screenshots = new List<string>();

        if (Directory.Exists(screenshotsPath))
        {
            screenshots = Directory.GetFiles(screenshotsPath, "*.png")
                .Select(f => Path.GetFileName(f))
                .OrderByDescending(f => f)
                .ToList();
        }

        return View(screenshots);
    }

    [HttpPost]
    public IActionResult ClearScreenshots()
    {
        var screenshotsPath = Path.Combine(_environment.WebRootPath, "screenshots");

        if (Directory.Exists(screenshotsPath))
        {
            var files = Directory.GetFiles(screenshotsPath, "*.png");
            foreach (var file in files)
            {
                System.IO.File.Delete(file);
            }
            _logger.LogInformation("Cleared {Count} screenshots", files.Length);
        }

        return RedirectToAction("Screenshots");
    }
}
