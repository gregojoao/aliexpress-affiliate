using AliExpress.Affiliate.Reports.Clients;
using AliExpress.Affiliate.Reports.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AliExpress.Affiliate.Reports.DependencyInjection;

/// <summary>
/// Dependency injection helpers for registering <see cref="IAliExpressAffiliateReportsClient"/>.
/// </summary>
public static class AliExpressAffiliateReportsServiceCollectionExtensions
{
    /// <summary>Default <see cref="IConfiguration"/> section bound to <see cref="AliExpressAffiliateReportsOptions"/>.</summary>
    public const string DefaultConfigurationSectionName = "AliExpress:Affiliate:Reports";

    public static IServiceCollection AddAliExpressAffiliateReports(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        AddClient(services);
        return services;
    }

    public static IServiceCollection AddAliExpressAffiliateReports(
        this IServiceCollection services,
        Action<AliExpressAffiliateReportsOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        AddClient(services);
        return services;
    }

    public static IServiceCollection AddAliExpressAffiliateReports(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddAliExpressAffiliateReports(configuration, DefaultConfigurationSectionName);
    }

    public static IServiceCollection AddAliExpressAffiliateReports(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.Configure<AliExpressAffiliateReportsOptions>(configuration.GetSection(sectionName));
        AddClient(services);
        return services;
    }

    private static void AddClient(IServiceCollection services)
    {
        services.AddOptions<AliExpressAffiliateReportsOptions>();
        services.AddHttpClient(nameof(AliExpressAffiliateReportsClient));
        services.AddTransient<AliExpressAffiliateReportsClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var options = sp.GetRequiredService<IOptions<AliExpressAffiliateReportsOptions>>();
            var logger = sp.GetService<ILogger<AliExpressAffiliateReportsClient>>();

            return new AliExpressAffiliateReportsClient(
                httpClientFactory.CreateClient(nameof(AliExpressAffiliateReportsClient)),
                options,
                logger);
        });
        services.AddTransient<IAliExpressAffiliateReportsClient>(sp =>
            sp.GetRequiredService<AliExpressAffiliateReportsClient>());
    }
}
