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
    /// Gets the extra information to include in the OPA policy evaluation.
    /// </summary>
    public string? ExtraInformation { get; }

    /// <summary>
    /// Gets the list of policy names to include in the OPA input.
    /// </summary>
    public IEnumerable<string> Policies { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizationRequirement"/> class.
    /// </summary>
    /// <param name="policies">The list of OPA policy names.</param>
    /// <param name="policyPath">The optional OPA policy path to evaluate.</param>
    /// <param name="extraInformation">Optional extra information to include in the policy evaluation.</param>
    public OpaAuthorizationRequirement(IEnumerable<string>? policies = null, string? policyPath = null, string? extraInformation = null)
    {
        Policies = policies ?? Enumerable.Empty<string>();
        PolicyPath = policyPath;
        ExtraInformation = extraInformation;
    }
}
