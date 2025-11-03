using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Extension methods for setting up OPA authorization services in an <see cref="IServiceCollection" />.
/// </summary>
public static class OpaAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds OPA authorization services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddOpaAuthorization(this IServiceCollection services)
    {
        return services.AddOpaAuthorization(_ => { });
    }

    /// <summary>
    /// Adds OPA authorization services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configureOptions">An <see cref="Action{OpaAuthorizationOptions}"/> to configure the provided <see cref="OpaAuthorizationOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddOpaAuthorization(
        this IServiceCollection services,
        Action<OpaAuthorizationOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        // Configure options with validation
        services.Configure(configureOptions);
        services.AddOptions<OpaAuthorizationOptions>()
            .Validate(options =>
            {
                try
                {
                    options.Validate();
                    return true;
                }
                catch
                {
                    return false;
                }
            }, "OpaAuthorizationOptions validation failed. Please check your configuration.");

        // Add HttpContextAccessor if not already registered
        services.TryAddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();

        // Register the authorization handler
        services.AddSingleton<IAuthorizationHandler, OpaAuthorizationHandler>();

        // Add authorization policy
        services.AddAuthorization(options =>
        {
            options.AddPolicy(OpaAuthorizationDefaults.PolicyName, policy =>
            {
                policy.Requirements.Add(new OpaAuthorizationRequirement());
            });
        });

        return services;
    }

    /// <summary>
    /// Adds a custom context data provider for OPA authorization.
    /// </summary>
    /// <typeparam name="TProvider">The type of the context data provider.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddOpaContextDataProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IOpaContextDataProvider
    {
        services.TryAddSingleton<IOpaContextDataProvider, TProvider>();
        return services;
    }
}
