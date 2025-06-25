namespace superScrape.Models;

public class BusinessListing
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Hours { get; set; } = string.Empty;
    public string PriceLevel { get; set; } = string.Empty;
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

public class ScrapingRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 20;
}

public class ScrapingResult
{
    public List<BusinessListing> Businesses { get; set; } = new();
    public string Query { get; set; } = string.Empty;
    public int TotalFound { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}
