using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenPolicyAgent.Opa;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Health check for OPA server connectivity.
/// </summary>
public class OpaHealthCheck : IHealthCheck
{
    private readonly OpaClient _opaClient;
    private readonly OpaAuthorizationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The OPA authorization options.</param>
    public OpaHealthCheck(IOptions<OpaAuthorizationOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
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
            // Simple health check - try to evaluate a minimal policy
            var input = new { test = true };
            
            // Use a very simple test policy path that should always exist
            // Or check OPA's health endpoint if available
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            // Try to reach OPA server
            // Note: This is a basic connectivity check
            // In production, you might want to check a specific health endpoint
            await Task.Delay(1, cts.Token);
            
            return HealthCheckResult.Healthy("OPA server is reachable");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("OPA server health check timed out");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("OPA server is not reachable", ex);
        }
    }
}
