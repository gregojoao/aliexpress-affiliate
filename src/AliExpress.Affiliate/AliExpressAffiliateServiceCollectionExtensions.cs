using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AliExpress.Affiliate;

/// <summary>
/// Dependency injection helpers for registering <see cref="AliExpressAffiliateClient"/>.
/// </summary>
public static class AliExpressAffiliateServiceCollectionExtensions
{
    public const string DefaultConfigurationSectionName = "AliExpress:Affiliate";

    public static IServiceCollection AddAliExpressAffiliate(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        AddClient(services);

        return services;
    }

    public static IServiceCollection AddAliExpressAffiliate(
        this IServiceCollection services,
        Action<AliExpressAffiliateOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        AddClient(services);

        return services;
    }

    public static IServiceCollection AddAliExpressAffiliate(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddAliExpressAffiliate(
            configuration,
            DefaultConfigurationSectionName);
    }

    public static IServiceCollection AddAliExpressAffiliate(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.Configure<AliExpressAffiliateOptions>(configuration.GetSection(sectionName));
        AddClient(services);

        return services;
    }

    private static void AddClient(IServiceCollection services)
    {
        services.AddOptions<AliExpressAffiliateOptions>();
        services.AddHttpClient(nameof(AliExpressAffiliateClient));
        services.AddTransient(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var options = sp.GetRequiredService<IOptions<AliExpressAffiliateOptions>>();

            return new AliExpressAffiliateClient(
                httpClientFactory.CreateClient(nameof(AliExpressAffiliateClient)),
                options);
        });
    }
}
