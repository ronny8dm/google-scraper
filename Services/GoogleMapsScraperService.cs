using Microsoft.Playwright;
using superScrape.Models;
using System.Text.RegularExpressions;
using System.Web;

namespace superScrape.Services;

public interface IGoogleMapsScraperService
{
    Task<ScrapingResult> ScrapBusinessListingsAsync(string query, int maxResults = 20);
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

    public async Task<ScrapingResult> ScrapBusinessListingsAsync(string query, int maxResults = 20)
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
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
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
                Permissions = new[] { "geolocation" },
                Geolocation = new Geolocation { Latitude = 51.5074f, Longitude = -0.1278f } // London coordinates
            });

            page = await context.NewPageAsync();

            // Add Google authentication and session cookies
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

          
            await page.AddInitScriptAsync(@"
                // Remove webdriver property
                Object.defineProperty(navigator, 'webdriver', {
                    get: () => undefined,
                });
                
                // Remove automation indicators
                delete navigator.__proto__.webdriver;
                
                // Override chrome runtime
                if (window.chrome && window.chrome.runtime) {
                    delete window.chrome.runtime.onConnect;
                    delete window.chrome.runtime.onMessage;
                }
                
                // Mock chrome object more thoroughly
                window.chrome = {
                    runtime: {},
                    loadTimes: function() {
                        return {
                            commitLoadTime: performance.timeOrigin + performance.now(),
                            connectionInfo: 'h2',
                            finishDocumentLoadTime: performance.timeOrigin + performance.now(),
                            finishLoadTime: performance.timeOrigin + performance.now(),
                            firstPaintAfterLoadTime: 0,
                            firstPaintTime: performance.timeOrigin + performance.now(),
                            navigationType: 'Other',
                            npnNegotiatedProtocol: 'h2',
                            requestTime: performance.timeOrigin,
                            startLoadTime: performance.timeOrigin + performance.now(),
                            wasAlternateProtocolAvailable: false,
                            wasFetchedViaSpdy: true,
                            wasNpnNegotiated: true
                        };
                    },
                    csi: function() {
                        return {
                            pageT: Date.now(),
                            tran: 15
                        };
                    }
                };
                
                // Override permissions API
                const originalQuery = window.navigator.permissions.query;
                window.navigator.permissions.query = (parameters) => (
                    parameters.name === 'notifications' ?
                        Promise.resolve({ state: 'denied' }) :
                        originalQuery(parameters)
                );
                
                // Override plugins
                Object.defineProperty(navigator, 'plugins', {
                    get: () => [1, 2, 3, 4, 5]
                });
                
                // Override languages
                Object.defineProperty(navigator, 'languages', {
                    get: () => ['en-US', 'en']
                });
                
                // Mock getTimezoneOffset
                Date.prototype.getTimezoneOffset = function() {
                    return -300; // EST timezone
                };
            ");

            // Add random delays to mimic human behavior
            await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Accept-Encoding", "gzip, deflate, br" },
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" },
                { "Cache-Control", "no-cache" },
                { "Pragma", "no-cache" }
            });

           
            _logger.LogInformation("Establishing session by visiting Google homepage");
            
            var maxRetries = 3;
            var retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    await page.GotoAsync("https://www.google.com", new PageGotoOptions 
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
                    _logger.LogWarning("Attempt {Attempt} failed to load Google homepage: {Error}", retryCount, ex.Message);
                    
                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"Failed to load Google homepage after {maxRetries} attempts: {ex.Message}");
                    }
                    
                    
                    await Task.Delay(Random.Shared.Next(2000, 5000));
                }
            }
            
      
            
            
            await HandleGoogleConsentPopup(page);
            
            
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
            
           
            await HandleGoogleConsentPopup(page);
            
           
            await page.Mouse.WheelAsync(0, Random.Shared.Next(100, 300));
            await Task.Delay(Random.Shared.Next(500, 1500));

            

           
            try
            {
                await page.WaitForSelectorAsync(".m6QErb", new PageWaitForSelectorOptions { Timeout = 20000 });
            }
            catch (TimeoutException)
            {
            
                
                
                var alternativeSelectors = new[]
                {
                    ".m6QErb", 
                    "[role='main']", 
                    ".Nv2PK", 
                    ".section-result", 
                    "[data-result-index]" 
                };

                var foundSelector = false;
                foreach (var selector in alternativeSelectors)
                {
                    try
                    {
                        await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 3000 });
                        _logger.LogInformation("Found alternative selector: {Selector}", selector);
                        foundSelector = true;
                        break;
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                }

                if (!foundSelector)
                {
                    throw new TimeoutException("Could not find search results with any known selector");
                }
            }

        

            
            await ScrollToLoadResults(page, maxResults);

            await TakeScreenshot(page, $"loaded_results_{GetSafeFileName(query)}");
            

            
            var businesses = await ExtractBusinessListings(page, maxResults);

            

            result.Businesses = businesses;
            result.TotalFound = businesses.Count;
            result.Success = true;

            _logger.LogInformation("Successfully scraped {Count} businesses for query: {Query}", businesses.Count, query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Google Maps for query: {Query}", query);
            result.ErrorMessage = ex.Message;
            
            
            if (page != null)
            {
                try
                {
                    await TakeScreenshot(page, $"error_state_{GetSafeFileName(query)}");
                }
                catch (Exception screenshotEx)
                {
                    _logger.LogWarning(screenshotEx, "Failed to take error screenshot");
                }
            }
        }

        return result;
    }

    private async Task ScrollToLoadResults(IPage page, int maxResults)
    {
        _logger.LogInformation("Starting to scroll for more results, target: {MaxResults}", maxResults);
        
        int lastCount = 0;
        int sameCountTimes = 0;
        int maxScrolls = 25;

        for (int i = 0; i < maxScrolls; i++)
        {
     
            var elements = await page.QuerySelectorAllAsync(".Nv2PK");
            _logger.LogInformation("Scroll {ScrollCount}: Found {ElementCount} elements", i + 1, elements.Count);
            
            if (elements.Count >= maxResults)
            {
                _logger.LogInformation("Reached target number of results: {Count}", elements.Count);
                break;
            }

            if (elements.Count == lastCount)
            {
                sameCountTimes++;
                _logger.LogInformation("No new results after scroll {ScrollCount}, same count times: {SameCountTimes}", i + 1, sameCountTimes);
            }
            else
            {
                sameCountTimes = 0;
            }

            lastCount = elements.Count;

            if (sameCountTimes > 3)
            {
                _logger.LogInformation("No new results after {SameCountTimes} scrolls, stopping", sameCountTimes);
                break;
            }


            var scrollSuccess = false;
            
            try
            {
         
                scrollSuccess = await page.EvaluateAsync<bool>(@"
                    () => {
                        const scrollElement = document.evaluate('//*[@id=""QA0Szd""]/div/div/div[1]/div[2]/div', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (scrollElement && scrollElement.scrollHeight > scrollElement.clientHeight) {
                            const oldScrollTop = scrollElement.scrollTop;
                            scrollElement.scrollBy(0, 500);
                            return scrollElement.scrollTop > oldScrollTop;
                        }
                        return false;
                    }
                ");

                if (scrollSuccess) {
                    _logger.LogInformation("Successfully scrolled using XPath method");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("XPath scroll failed: {Error}", ex.Message);
            }

            if (!scrollSuccess)
            {
                try
                {
                    scrollSuccess = await page.EvaluateAsync<bool>(@"
                        () => {
                            const selectors = [
                                '#QA0Szd > div > div > div:nth-child(1) > div:nth-child(2) > div',
                                '.m6QErb',
                                '[role=""main""]',
                                '.TFQHme',
                                '.Nv2PK'
                            ];
                            
                            for (const selector of selectors) {
                                const element = document.querySelector(selector);
                                if (element && element.scrollHeight > element.clientHeight) {
                                    const oldScrollTop = element.scrollTop;
                                    element.scrollBy(0, 500);
                                    if (element.scrollTop > oldScrollTop) {
                                        console.log('Scrolled with selector: ' + selector);
                                        return true;
                                    }
                                }
                            }
                            return false;
                        }
                    ");

                    if (scrollSuccess) {
                        _logger.LogInformation("Successfully scrolled using CSS selector method");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("CSS selector scroll failed: {Error}", ex.Message);
                }
            }

            if (!scrollSuccess)
            {
                try
                {
                    scrollSuccess = await page.EvaluateAsync<bool>(@"
                        () => {
                            const resultElements = document.querySelectorAll('.Nv2PK');
                            if (resultElements.length > 0) {
                                let parent = resultElements[0].parentElement;
                                while (parent && parent !== document.body) {
                                    if (parent.scrollHeight > parent.clientHeight) {
                                        const oldScrollTop = parent.scrollTop;
                                        parent.scrollBy(0, 500);
                                        if (parent.scrollTop > oldScrollTop) {
                                            console.log('Scrolled parent element');
                                            return true;
                                        }
                                    }
                                    parent = parent.parentElement;
                                }
                            }
                            return false;
                        }
                    ");

                    if (scrollSuccess) {
                        _logger.LogInformation("Successfully scrolled using parent element method");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Parent element scroll failed: {Error}", ex.Message);
                }
            }


            if (!scrollSuccess)
            {
                try
                {
                    await page.EvaluateAsync("window.scrollBy(0, 500)");
                    _logger.LogInformation("Used fallback window scroll");
                    scrollSuccess = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "All scroll methods failed on attempt {ScrollCount}", i + 1);
                }
            }

         
            await page.WaitForTimeoutAsync(3000); 
            
    
            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 3000 });
            }
            catch (TimeoutException)
            {
          
            }
        }
        
        var finalElements = await page.QuerySelectorAllAsync(".Nv2PK");
        _logger.LogInformation("Finished scrolling. Final count: {FinalCount} elements", finalElements.Count);
    }

    private async Task<List<BusinessListing>> ExtractBusinessListings(IPage page, int maxResults)
    {
        var businesses = new List<BusinessListing>();
        const int BATCH_SIZE = 3; 

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

            var processedCount = 0;
            var batchNumber = 0;
            var consecutiveEmptyBatches = 0;
            var maxConsecutiveEmptyBatches = 3;

            while (processedCount < maxResults)
            {
                batchNumber++;
                _logger.LogInformation("Starting batch {BatchNumber} (processed: {ProcessedCount}/{MaxResults})", 
                    batchNumber, processedCount, maxResults);

            
                var businessElements = await page.QuerySelectorAllAsync(".Nv2PK");
                _logger.LogInformation("Found {Count} total business elements in batch {BatchNumber}", 
                    businessElements.Count, batchNumber);

                if (businessElements.Count == 0)
                {
                    _logger.LogWarning("No business elements found, stopping extraction");
                    break;
                }

                if (processedCount >= businessElements.Count)
                {
                    _logger.LogInformation("All available businesses ({Count}) have been processed. Stopping extraction.", businessElements.Count);
                    break;
                }

               
                var batchStart = processedCount;
                var batchEnd = Math.Min(batchStart + BATCH_SIZE, Math.Min(businessElements.Count, maxResults));
                
                var batchProcessedCount = 0;

                for (int i = batchStart; i < batchEnd; i++)
                {
                    try
                    {
                        if (i >= businessElements.Count)
                        {
                            _logger.LogWarning("Index {Index} exceeds available elements {Count}", i, businessElements.Count);
                            break;
                        }

                        var element = businessElements[i];
                        
                        
                        try
                        {
                            var isConnected = await element.EvaluateAsync<bool>("el => el.isConnected");
                            if (!isConnected)
                            {
                                _logger.LogWarning("Element at index {Index} is disconnected, re-querying", i);
                                businessElements = await page.QuerySelectorAllAsync(".Nv2PK");
                                if (i >= businessElements.Count) continue;
                                element = businessElements[i];
                            }
                        }
                        catch (Exception)
                        {
                          
                            businessElements = await page.QuerySelectorAllAsync(".Nv2PK");
                            if (i >= businessElements.Count) continue;
                            element = businessElements[i];
                        }

                        var business = await ExtractBusinessDetails(element, page);
                        
                        if (business != null)
                        {
                            businesses.Add(business);
                            processedCount++;
                            batchProcessedCount++;
                            _logger.LogInformation("Successfully processed business {Count}/{Max}: {Name}", 
                                processedCount, maxResults, business.Name);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to extract business at index {Index}", i);
                        }

                       
                        await page.WaitForTimeoutAsync(500);

                        
                        if (processedCount >= maxResults)
                        {
                            _logger.LogInformation("Reached target of {MaxResults} businesses", maxResults);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing business at index {Index}", i);
                       
                    }
                }

             
                if (batchProcessedCount == 0)
                {
                    consecutiveEmptyBatches++;
                    _logger.LogWarning("Batch {BatchNumber} processed 0 businesses. Consecutive empty batches: {Count}", 
                        batchNumber, consecutiveEmptyBatches);
                    
                    if (consecutiveEmptyBatches >= maxConsecutiveEmptyBatches)
                    {
                        _logger.LogInformation("Too many consecutive empty batches ({Count}). Likely reached end of available results.", 
                            consecutiveEmptyBatches);
                        break;
                    }
                }
                else
                {
                    consecutiveEmptyBatches = 0; 
                }

              
                if (processedCount >= maxResults)
                {
                    break;
                }

                if (processedCount >= businessElements.Count)
                {
                    _logger.LogInformation("Processed all {Count} available businesses. Cannot reach target of {MaxResults}.", 
                        businessElements.Count, maxResults);
                    break;
                }

                
                if (processedCount < maxResults && batchNumber % 5 == 0)
                {
                    _logger.LogInformation("Performing state reset after batch {BatchNumber}", batchNumber);
                    
                   
                    await page.EvaluateAsync(@"
                        () => {
                            const container = document.querySelector('.m6QErb') || document.querySelector('#QA0Szd');
                            if (container) {
                                container.scrollTop = Math.max(0, container.scrollTop - 100);
                            }
                        }
                    ");
                    
                    await page.WaitForTimeoutAsync(2000);
                   
                    try
                    {
                        await page.WaitForSelectorAsync(".Nv2PK", new PageWaitForSelectorOptions 
                        { 
                            Timeout = 5000,
                            State = WaitForSelectorState.Attached 
                        });
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning("Lost business elements after batch {BatchNumber}, stopping", batchNumber);
                        break;
                    }
                }
                else
                {
                   
                    await page.WaitForTimeoutAsync(1000);
                }
            }

            _logger.LogInformation("Extraction completed. Processed {ProcessedCount} out of requested {MaxResults} businesses", 
                processedCount, maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting business listings");
        }

        return businesses;
    }

    private async Task<BusinessListing?> ExtractBusinessDetails(IElementHandle element, IPage page)
    {
        try
        {
            var business = new BusinessListing();

            
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

       
            var addressElements = await element.QuerySelectorAllAsync(".W4Efsd .W4Efsd");
            if (addressElements.Count > 0)
            {
                var addressText = await addressElements[0].TextContentAsync();
                business.Address = addressText?.Trim() ?? string.Empty;
            }

            var categoryElement = await element.QuerySelectorAsync(".W4Efsd:not(.W4Efsd .W4Efsd)");
            if (categoryElement != null)
            {
                business.Category = (await categoryElement.TextContentAsync())?.Trim() ?? string.Empty;
            }

            
            try
            {
                _logger.LogInformation("Starting to extract details for business: {Name}", business.Name);
                
              
                var originalUrl = page.Url;
               
                await element.ClickAsync(new ElementHandleClickOptions { Timeout = 10000 });
          
                try
                {
                    await Task.WhenAny(
                        page.WaitForFunctionAsync("(originalUrl) => window.location.href !== originalUrl", originalUrl, new PageWaitForFunctionOptions { Timeout = 5000 }),
                        page.WaitForSelectorAsync("[role='main'] h1, .x3AX1-LfntMc-header-title, .rogA2c", new PageWaitForSelectorOptions { Timeout = 5000 })
                    );
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Neither URL change nor details panel detected for: {Name}", business.Name);
                }

            
                if (page.Url != originalUrl)
                {
                    
                    business.GoogleMapsUrl = page.Url;
                    _logger.LogInformation("Navigated to business page: {Url}", page.Url);
                    
                    await ExtractAdditionalDetails(page, business);
                    await page.GoBackAsync();
                    await page.WaitForSelectorAsync(".Nv2PK", new PageWaitForSelectorOptions { Timeout = 10000 });
                }
                else
                {
                   
                    _logger.LogInformation("Details panel opened for business: {Name}", business.Name);
                    business.GoogleMapsUrl = page.Url; 
                    await ExtractAdditionalDetails(page, business);
                    
                    
                    await page.Keyboard.PressAsync("Escape");
                    await page.WaitForTimeoutAsync(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract additional details for business: {Name}", business.Name);
            }

            return business;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting business details");
            return null;
        }
    }

    private async Task ExtractAdditionalDetails(IPage page, BusinessListing business)
    {
        try
        {
           
            await page.WaitForTimeoutAsync(2000);

            var phoneElements = await page.QuerySelectorAllAsync("[data-value*='phone'], [href^='tel:']");
            foreach (var phoneElement in phoneElements)
            {
                var phoneText = await phoneElement.TextContentAsync();
                if (!string.IsNullOrEmpty(phoneText) && Regex.IsMatch(phoneText, @"[\d\s\-\+\(\)]+"))
                {
                    business.Phone = phoneText.Trim();
                    break;
                }
            }

           
            var websiteElements = await page.QuerySelectorAllAsync("[data-value*='website'], [href^='http']:not([href*='google.com'])");
            foreach (var websiteElement in websiteElements)
            {
                var href = await websiteElement.GetAttributeAsync("href");
                if (!string.IsNullOrEmpty(href) && !href.Contains("google.com"))
                {
                    business.Website = href;
                    break;
                }
            }

           
            var hoursElement = await page.QuerySelectorAsync("[data-value*='hours']");
            if (hoursElement != null)
            {
                business.Hours = (await hoursElement.TextContentAsync())?.Trim() ?? string.Empty;
            }

            
            var descriptionElement = await page.QuerySelectorAsync("[data-value*='description']");
            if (descriptionElement != null)
            {
                business.Description = (await descriptionElement.TextContentAsync())?.Trim() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting additional details");
        }
    }

    private async Task TakeScreenshot(IPage page, string filename)
    {
        try
        {
            var screenshotPath = Path.Combine(_environment.WebRootPath, "screenshots", $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });
            
            _logger.LogInformation("Screenshot saved: {Path}", screenshotPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to take screenshot: {Filename}", filename);
        }
    }

    private string GetSafeFileName(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(input.Where(c => !invalidChars.Contains(c)).ToArray())
            .Replace(" ", "_")
            .ToLowerInvariant();
    }

    private async Task HandleGoogleConsentPopup(IPage page)
    {
        try
        {
            _logger.LogInformation("Checking for Google consent popup...");
            
            
            await Task.Delay(1000);
            
         
            var acceptButtonSelectors = new[]
            {
                "button[jsname='b3VHJd']",
                "button[aria-label='Accept all']",
                "button:has-text('Accept all')",
                "button.UywwFc-LgbsSe:has-text('Accept all')", 
                "[data-testid='accept-all-button']", 
                "button[data-idom-class='XWZjwc']", 
                "//button[contains(text(), 'Accept all')]", 
                "//button[@aria-label='Accept all']" 
            };

            bool popupHandled = false;

            foreach (var selector in acceptButtonSelectors)
            {
                try
                {
                    var button = await page.QuerySelectorAsync(selector);
                    if (button != null)
                    {
                        var isVisible = await button.IsVisibleAsync();
                        if (isVisible)
                        {
                            _logger.LogInformation("Found consent popup button with selector: {Selector}", selector);
                            
                        
                            
                           
                            await button.ScrollIntoViewIfNeededAsync();
                            await Task.Delay(500);
                            
                           
                            await button.ClickAsync();
                            _logger.LogInformation("Clicked 'Accept all' button");
                            
                           
                            await Task.Delay(2000);
                            
                          
                            
                            popupHandled = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Selector {Selector} failed: {Error}", selector, ex.Message);
                    
                }
            }

           
            if (!popupHandled)
            {
                var genericSelectors = new[]
                {
                    "button:has-text('Accept')",
                    "button:has-text('OK')",
                    "button:has-text('Continue')",
                    "button:has-text('Agree')",
                    "[role='button']:has-text('Accept')"
                };

                foreach (var selector in genericSelectors)
                {
                    try
                    {
                        var button = await page.QuerySelectorAsync(selector);
                        if (button != null && await button.IsVisibleAsync())
                        {
                            _logger.LogInformation("Found generic consent button with selector: {Selector}", selector);
                            await button.ClickAsync();
                            await Task.Delay(1000);
                            popupHandled = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Generic selector {Selector} failed: {Error}", selector, ex.Message);
                    }
                }
            }

            if (popupHandled)
            {
                _logger.LogInformation("Successfully handled Google consent popup");
            }
            else
            {
                _logger.LogInformation("No consent popup found or already handled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling Google consent popup");
            
        }
    }
}
