using Microsoft.Playwright;
using superScrape.Models;
using System.Text.RegularExpressions;
using System.Web;
using superScrape.Services; 

namespace superScrape.Services;

public interface IGoogleMapsScraperService
{
    Task<ScrapingResult> ScrapBusinessListingsAsync(string query, int maxResults = 100);
}

public class GoogleMapsScraperService : IGoogleMapsScraperService
{
    private readonly ILogger<GoogleMapsScraperService> _logger;
    private readonly IWebHostEnvironment _environment;
    

    public GoogleMapsScraperService(ILogger<GoogleMapsScraperService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
      
    }

    public async Task<ScrapingResult> ScrapBusinessListingsAsync(string query, int maxResults = 100)
    {
        var result = new ScrapingResult
        {
            Query = query,
            Success = false
        };

        IPage? page = null;

        try
        {
            _logger.LogInformation("Starting to scrape Google Maps for query: {Query}", query);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                SlowMo = 100,
                Args = new[] {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-blink-features=AutomationControlled",
                    "--disable-web-security",
                    "--disable-features=VizDisplayCompositor",
                    "--disable-extensions",
                    "--no-first-run",
                    "--disable-default-apps",
                    "--disable-dev-shm-usage",
                    "--no-zygote",
                    "--disable-gpu",
                    "--disable-background-networking",
                    "--disable-background-timer-throttling",
                    "--disable-backgrounding-occluded-windows",
                    "--disable-renderer-backgrounding",
                    "--disable-features=TranslateUI",
                    "--disable-ipc-flooding-protection",
                    "--disable-client-side-phishing-detection",
                    "--disable-sync",
                    "--metrics-recording-only",
                    "--no-report-upload",
                    "--allow-running-insecure-content",
                    "--disable-component-update",
                    "--disable-domain-reliability",
                    "--start-maximized",
                }
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1366, Height = 768 }, // More common resolution
                Locale = "en-US",
                TimezoneId = "America/New_York",
                JavaScriptEnabled = true,
                AcceptDownloads = false,
                IgnoreHTTPSErrors = true,
                Permissions = new[] { "geolocation" }
            });

            page = await context.NewPageAsync();

            await context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "__Secure-1PAPISID",
                    Value = "7M-h77lBI_YIuJ23/AeO8htKoVyxMhsLvP",
                    Domain = ".google.com",
                    Path = "/",
                    Secure = true,
                    HttpOnly = false
                },

            });



            await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Accept-Encoding", "gzip, deflate, br" },
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" },
                { "Cache-Control", "no-cache" },
                { "Pragma", "no-cache" }
            });


            _logger.LogInformation("Establishing session by visiting Google homepage");

            var maxRetries = 5;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    await page.GotoAsync("https://www.google.com");
                    await page.WaitForURLAsync("https://www.google.com/", new PageWaitForURLOptions
                    {
                        Timeout = 30000,
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);



                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning("Attempt {Attempt} failed to load Google homepage: {Error}", retryCount, ex.Message);

                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"Failed to load Google homepage after {maxRetries} attempts: {ex.Message}");
                    }


                    await Task.Delay(Random.Shared.Next(2000, 5000));
                }
            }

            await page.ClickAsync("button:has-text('Accept all')");

            await Task.Delay(Random.Shared.Next(2000, 4000));

            await page.Mouse.MoveAsync(Random.Shared.Next(100, 800), Random.Shared.Next(100, 600));
            await Task.Delay(Random.Shared.Next(500, 1500));


            var encodedQuery = HttpUtility.UrlEncode(query);
            var url = $"https://www.google.com/maps/search/{encodedQuery}";

            _logger.LogInformation("Navigating to: {Url}", url);

            retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    });


                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning("Attempt {Attempt} failed to load Google Maps: {Error}", retryCount, ex.Message);

                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"Failed to load Google Maps after {maxRetries} attempts: {ex.Message}");
                    }


                    await Task.Delay(Random.Shared.Next(3000, 6000));
                }
            }


            await Task.Delay(Random.Shared.Next(2000, 4000));



            await page.Mouse.WheelAsync(0, Random.Shared.Next(100, 1000));
            await Task.Delay(Random.Shared.Next(500, 1500));


            IElementHandle? selectorElement = null;
            var foundSelector = false;

            try
            {
                selectorElement = await page.WaitForSelectorAsync(".m6QErb", new PageWaitForSelectorOptions { Timeout = 20000 });
                foundSelector = selectorElement != null;
            }
            catch (TimeoutException)
            {

                if (!foundSelector)
                {
                    throw new TimeoutException("Could not find search results with any known selector");
                }
            }

            var processedCount = 0;
            var businesses = new List<BusinessListing>();

            await page.Locator("[role=feed]").HoverAsync();

            // Keep scrolling until we find the end message OR have enough results
            while (!await page.Locator(".HlvSq:has-text(\"You've reached the end of the list\")").IsVisibleAsync())
            {
                // Check how many business elements are currently loaded
                var currentElementCount = await page.QuerySelectorAllAsync(".Nv2PK").ContinueWith(t => t.Result.Count);
                
                if (maxResults > 0 && currentElementCount >= maxResults)
                {
                    _logger.LogInformation("Found {Count} results, which meets the requirement of {MaxResults}. Stopping scroll.", currentElementCount, maxResults);
                    break;
                }
                
                await page.Mouse.WheelAsync(0, 800);
                await page.WaitForTimeoutAsync(1000);
            }

            await page.WaitForTimeoutAsync(1000);

            // Extract businesses after scrolling is complete
            businesses = await ExtractBusinessListings(page, maxResults);

            
            result.Businesses = businesses;
            result.TotalFound = businesses.Count;
            result.Success = true;

            _logger.LogInformation("Successfully scraped {Count} businesses for query: {Query}", businesses.Count, query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Google Maps for query: {Query}", query);
            result.ErrorMessage = ex.Message;

        }

        return result;
    }



    private async Task<List<BusinessListing>> ExtractBusinessListings(IPage page, int maxResults)
    {
        var businesses = new List<BusinessListing>();
        var uniqueBusinessKeys = new HashSet<string>();
        var actualElementCount = await page.QuerySelectorAllAsync(".Nv2PK").ContinueWith(t => t.Result.Count);

        // Set our target to the actual count we found
        var targetResults = actualElementCount;
        _logger.LogInformation("*** business counts ****, target: {ActualCount}, actual elements: {ActualCount}",
            targetResults, actualElementCount);
        const int BATCH_SIZE = 5;

        try
        {

            try
            {
                await page.WaitForSelectorAsync(".m6QErb", new PageWaitForSelectorOptions
                {
                    Timeout = 10000,
                    State = WaitForSelectorState.Attached
                });
            }
            catch (TimeoutException)
            {

                _logger.LogWarning("Main container .m6QErb not found, trying alternative selectors");

                var alternativeSelectors = new[] { ".Nv2PK", "[role='main']", ".section-result" };
                var found = false;

                foreach (var selector in alternativeSelectors)
                {
                    try
                    {
                        await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                        {
                            Timeout = 5000,
                            State = WaitForSelectorState.Attached
                        });
                        found = true;
                        break;
                    }
                    catch (TimeoutException) { continue; }
                }

                if (!found)
                {
                    throw new TimeoutException("No business listings container found");
                }
            }

            var businessElements = await page.QuerySelectorAllAsync(".Nv2PK");
            _logger.LogInformation("Found {Count} business elements to process", businessElements.Count);

            // Simple loop through all found businesses
            for (int i = 0; i < targetResults; i++)
            {
                try
                {
                    businessElements = await page.QuerySelectorAllAsync(".Nv2PK");
                    _logger.LogInformation("Processing business {Index}/{Total}", i + 1, Math.Min(businessElements.Count, targetResults));

                    if (i >= businessElements.Count)
                        {
                            _logger.LogWarning("Index {Index} exceeds available elements {Count}. Stopping extraction.", i, businessElements.Count);
                            break;
                        }
                    _logger.LogInformation("Processing business {Index}/{Total}", i + 1, Math.Min(businessElements.Count, targetResults));

                    var element = businessElements[i];
                    _logger.LogInformation("Processing business {Index}/{Total}", i + 1, Math.Min(businessElements.Count, targetResults));

                    // Check if element is still connected to DOM
                    var isConnected = await element.EvaluateAsync<bool>("el => el.isConnected");
                    if (!isConnected)
                    {
                        _logger.LogWarning("Element at index {Index} is disconnected, skipping", i);
                        continue;
                    }

                    var business = new BusinessListing();

                    // Extract basic details from the card
                    var nameElement = await element.QuerySelectorAsync(".qBF1Pd");
                    if (nameElement != null)
                    {
                        business.Name = (await nameElement.TextContentAsync())?.Trim() ?? string.Empty;
                    }

                    var ratingElement = await element.QuerySelectorAsync(".MW4etd");
                    if (ratingElement != null)
                    {
                        var ratingText = await ratingElement.TextContentAsync();
                        if (double.TryParse(ratingText?.Replace(",", "."), out var rating))
                        {
                            business.Rating = rating;
                        }
                    }

                    var reviewElement = await element.QuerySelectorAsync(".UY7F9");
                    if (reviewElement != null)
                    {
                        var reviewText = await reviewElement.TextContentAsync();
                        var match = Regex.Match(reviewText ?? "", @"\((\d+(?:,\d+)*)\)");
                        if (match.Success && int.TryParse(match.Groups[1].Value.Replace(",", ""), out var reviewCount))
                        {
                            business.ReviewCount = reviewCount;
                        }
                    }

                    var infoContainer = await element.QuerySelectorAsync(".W4Efsd .W4Efsd");
                    if (infoContainer != null)
                    {
                        var spans = await infoContainer.QuerySelectorAllAsync("span");
                        if (spans.Count >= 1)
                        {
                            var categoryText = await spans[0].TextContentAsync();
                            business.Category = categoryText?.Trim() ?? string.Empty;
                        }
                    }

                    // Click to get additional details
                    try
                    {
                        var originalUrl = page.Url;
                        await element.ClickAsync(new ElementHandleClickOptions { Timeout = 10000 });

                        // Wait for details panel or URL change
                        await Task.WhenAny(
                            page.WaitForFunctionAsync("(originalUrl) => window.location.href !== originalUrl", originalUrl, new PageWaitForFunctionOptions { Timeout = 5000 }),
                            page.WaitForSelectorAsync("[role='main'] h1, .x3AX1-LfntMc-header-title, .rogA2c", new PageWaitForSelectorOptions { Timeout = 5000 })
                        );

                        await page.WaitForTimeoutAsync(2000);
                        business.GoogleMapsUrl = page.Url;

                        // Extract additional details inline
                        var addressElements = await page.QuerySelectorAllAsync("[aria-label*='Address:']");
                        foreach (var addressElement in addressElements)
                        {
                            var ariaLabel = await addressElement.GetAttributeAsync("aria-label");
                            if (!string.IsNullOrEmpty(ariaLabel) && ariaLabel.StartsWith("Address:"))
                            {
                                business.Address = ariaLabel.Replace("Address:", "").Trim();
                                break;
                            }
                        }

                        var phoneElements = await page.QuerySelectorAllAsync("[data-item-id*='phone:tel:']");
                        foreach (var phoneElement in phoneElements)
                        {
                            var dataItemId = await phoneElement.GetAttributeAsync("data-item-id");
                            if (!string.IsNullOrEmpty(dataItemId) && dataItemId.StartsWith("phone:tel:"))
                            {
                                var phoneNumber = dataItemId.Replace("phone:tel:", "").Trim();
                                if (!string.IsNullOrEmpty(phoneNumber) && Regex.IsMatch(phoneNumber, @"^[\+]?[\d\s\-\(\)\.]{7,}$"))
                                {
                                    business.Phone = phoneNumber;
                                    break;
                                }
                            }
                        }

                        var websiteElements = await page.QuerySelectorAllAsync("[aria-label*='Website:']");
                        foreach (var websiteElement in websiteElements)
                        {
                            var ariaLabel = await websiteElement.GetAttributeAsync("aria-label");
                            if (!string.IsNullOrEmpty(ariaLabel) && ariaLabel.StartsWith("Website:"))
                            {
                                var website = ariaLabel.Replace("Website:", "").Trim();
                                if (!website.StartsWith("http://") && !website.StartsWith("https://"))
                                {
                                    website = "https://" + website;
                                }
                                business.Website = website;
                                break;
                            }
                        }

                        // Navigate back to list
                        var currentUrl = page.Url;
                        if (currentUrl != originalUrl)
                        {
                            // await page.GoBackAsync();
                            await page.WaitForSelectorAsync(".Nv2PK", new PageWaitForSelectorOptions { Timeout = 10000 });
                            // Refresh elements after navigation
                            businessElements = await page.QuerySelectorAllAsync(".Nv2PK");
                        }
                        else
                        {
                            await page.Keyboard.PressAsync("Escape");
                            await page.WaitForTimeoutAsync(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not extract additional details for business: {Name}", business.Name);
                    }

                    // Add to results if unique
                    if (!string.IsNullOrEmpty(business.Name))
                    {
                        string businessKey = GenerateBusinessKey(business);
                        if (!uniqueBusinessKeys.Contains(businessKey))
                        {
                            uniqueBusinessKeys.Add(businessKey);
                            businesses.Add(business);
                            _logger.LogInformation("Successfully processed business {Count}: {Name}", businesses.Count, business.Name);
                        }
                        else
                        {
                            _logger.LogInformation("Skipped duplicate business: {Name}", business.Name);
                        }
                    }

                    if (businesses.Count >= maxResults)
                    {
                        _logger.LogInformation("Reached target of {MaxResults} unique businesses", maxResults);
                        break;
                    }

                    await page.WaitForTimeoutAsync(500);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing business at index {Index}", i);
                }
            }

            _logger.LogInformation("Extraction completed. Processed {Count} unique businesses", businesses.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting business listings");
        }

        return businesses;
    }

    private string GenerateBusinessKey(BusinessListing business)
    {
        return $"{business.Name?.ToLowerInvariant()}|{business.Address?.ToLowerInvariant()}|{business.Phone}";
    }

    private string GetSafeFileName(string input)
    {
        return Regex.Replace(input, @"[<>:""/\\|?*]", "_").Replace(" ", "_");
    }
}
