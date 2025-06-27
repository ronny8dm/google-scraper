using Microsoft.Extensions.DependencyInjection;

namespace superScrape.Services;

public interface IScraperServiceFactory
{
    IGoogleMapsScraperService CreateScraperService();
}

public class ScraperServiceFactory : IScraperServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ScraperServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IGoogleMapsScraperService CreateScraperService()
    {
        // Create a new scope for each request to the factory
        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IGoogleMapsScraperService>();
    }
}