using superScrape.Services;
using AspNetCoreRateLimit;
using System;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";

builder.WebHost.UseUrls($"http://*:{port}");
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/scraperapi/scrape",
            Period = "1m",
            Limit = 3, // Only 3 scraping requests per minute per IP
        },
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m", 
            Limit = 30, // 30 requests per minute for all other endpoints
        }
    };
});

builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddScoped<IUtils, Utils>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IGoogleMapsScraperService, GoogleMapsScraperService>();

// 2. Then register the factory that depends on the service provider
builder.Services.AddSingleton<IScraperServiceFactory, ScraperServiceFactory>();

// 3. Finally register the background service that depends on the factory
builder.Services.AddSingleton<IBackgroundJobService, BackgroundJobService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", builder =>
    {
        builder.WithOrigins("https://superscrape.ronnyjdiaz.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); ;
    });
});

var app = builder.Build();

app.UseCors("ReactApp");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
