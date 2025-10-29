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
            // Create a simple test input
            var input = new { healthCheck = true };
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            // Try to evaluate a simple policy to verify OPA connectivity
            // Use a generic "health" path or default path
            // This will fail fast if OPA is not reachable
            try
            {
                // Attempt to call OPA - even if the policy doesn't exist, 
                // a reachable OPA server will respond (with 404 or valid response)
                await _opaClient.Evaluate<object>("health", input);
                
                return HealthCheckResult.Healthy("OPA server is reachable and responding");
            }
            catch (OpaException ex) when (ex.Message.Contains("404") || ex.Message.Contains("not found"))
            {
                // Policy not found is OK - it means OPA is reachable
                return HealthCheckResult.Healthy("OPA server is reachable (health policy not configured)");
            }
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
