using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Health check for OPA server connectivity.
/// </summary>
public class OpaHealthCheck : IHealthCheck
{
    private readonly OpaAuthorizationOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpaHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The OPA authorization options.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public OpaHealthCheck(
        IOptions<OpaAuthorizationOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<OpaHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Checks the health of the OPA server.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_options.DisableAuthorization)
        {
            return HealthCheckResult.Healthy("OPA authorization is disabled.");
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.RequestTimeout);

            var opaUrl = OpaServerUrlResolver.Resolve(_options);
            var healthUri = new Uri(new Uri(EnsureTrailingSlash(opaUrl)), "health");
            var client = _httpClientFactory.CreateClient();

            using var response = await client.GetAsync(
                healthUri,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("OPA server is reachable and responding.");
            }

            _logger.LogWarning(
                "OPA health endpoint returned status code {StatusCode}",
                response.StatusCode);

            return HealthCheckResult.Unhealthy(
                $"OPA health endpoint returned status code {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OPA health check failed: HTTP connection error");
            return HealthCheckResult.Unhealthy(
                $"Unable to connect to OPA server at {_options.OpaUrl}",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "OPA health check failed: Timeout");
            return HealthCheckResult.Unhealthy(
                $"OPA server at {_options.OpaUrl} did not respond within {_options.RequestTimeout}",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OPA health check failed: Unexpected error");
            return HealthCheckResult.Unhealthy(
                $"Unexpected error checking OPA server health: {ex.Message}",
                ex);
        }
    }

    private static string EnsureTrailingSlash(string url)
    {
        return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
    }
}
