using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPolicyAgent.Opa;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Health check for OPA server connectivity.
/// </summary>
public class OpaHealthCheck : IHealthCheck
{
    private readonly OpaAuthorizationOptions _options;
    private readonly ILogger<OpaHealthCheck> _logger;
    private readonly OpaClient _opaClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The OPA authorization options.</param>
    /// <param name="logger">The logger.</param>
    public OpaHealthCheck(IOptions<OpaAuthorizationOptions> options, ILogger<OpaHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        var opaUrl = _options.OpaUrl ?? Environment.GetEnvironmentVariable("OPA_URL") ?? "http://localhost:8181";
        _opaClient = new OpaClient(opaUrl);
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
        try
        {
            // Try a simple policy check to verify OPA is responsive
            // This uses a minimal input that should work with any OPA deployment
            var testInput = new { test = "health_check" };
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.RequestTimeout);

            // Try to evaluate a non-existent policy - we just want to verify connectivity
            // OPA will return an error but that's fine - it proves OPA is reachable
            try
            {
                await _opaClient.Check("data/nonexistent/healthcheck", testInput);
            }
            catch (OpaException ex)
            {
                // If we get an OpaException, it means we successfully connected to OPA
                // Even if the policy doesn't exist, OPA responded
                _logger.LogDebug("OPA health check received response: {Message}", ex.Message);
                return !ex.Message.Contains("succeeded") ? HealthCheckResult.Unhealthy("Opa did not response") : HealthCheckResult.Healthy("OPA server is reachable and responding");
            }

            // If we get here without an exception, OPA is healthy
            return HealthCheckResult.Healthy("OPA server is reachable and responding");
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
}
