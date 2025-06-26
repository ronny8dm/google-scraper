using superScrape.Services;
using Microsoft.Playwright;




public interface IUtils
{
    Task HandleGoogleConsentPopup(IPage page);
    Task ScrollToLoadResults(IPage page, int maxResults);
}
public class Utils : IUtils
{

    private readonly ILogger<GoogleMapsScraperService> _logger;

    public Utils(ILogger<GoogleMapsScraperService> logger)
    {
        _logger = logger;
    }

    public async Task HandleGoogleConsentPopup(IPage page)
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


    public async Task ScrollToLoadResults(IPage page, int maxResults)
    {
        _logger.LogInformation("Starting to scroll for more results, target: {MaxResults}", maxResults);


        const string resultsContainer = ".m6QErb > div:nth-child(1) > div:nth-child(2)";
        await page.EvaluateAsync($@"
  () => {{
    const el = document.querySelector('{resultsContainer}');
    if (el) el.scrollBy(0, 500);
  }}
");



        int lastCount = 0;
        int sameCountTimes = 0;
        int maxScrolls = 40;

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

                if (scrollSuccess)
                {
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

                    if (scrollSuccess)
                    {
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

                    if (scrollSuccess)
                    {
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
}

