using Microsoft.AspNetCore.Authorization;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Authorization requirement for OPA-based authorization.
/// </summary>
public class OpaAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the OPA policy path to evaluate for this requirement.
    /// </summary>
    public string? PolicyPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizationRequirement"/> class.
    /// </summary>
    /// <param name="policyPath">The optional OPA policy path to evaluate.</param>
    public OpaAuthorizationRequirement(string? policyPath = null)
    {
        PolicyPath = policyPath;
    }
}
