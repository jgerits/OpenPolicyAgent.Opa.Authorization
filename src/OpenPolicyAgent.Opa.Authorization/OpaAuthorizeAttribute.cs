using Microsoft.AspNetCore.Authorization;

namespace OpenPolicyAgent.Opa.Authorization;

/// <summary>
/// Specifies that the class or method that this attribute is applied to requires 
/// authorization via Open Policy Agent.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class OpaAuthorizeAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Gets or sets the OPA policy path to evaluate for this authorization decision.
    /// If not specified, the default policy path configured in OpaAuthorizationOptions will be used.
    /// </summary>
    public string? PolicyPath { get; set; }

    /// <summary>
    /// Gets the list of policy names to be sent to OPA for evaluation.
    /// </summary>
    public string[] Policies { get; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional information to include in the OPA policy evaluation.
    /// This data will be available under input.context.metadata in the OPA policy.
    /// </summary>
    public string? ExtraInformation { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizeAttribute"/> class.
    /// </summary>
    public OpaAuthorizeAttribute()
    {
        Policy = OpaAuthorizationDefaults.PolicyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpaAuthorizeAttribute"/> class with specified policy names.
    /// </summary>
    /// <param name="policies">The list of OPA policy names to evaluate.</param>
    public OpaAuthorizeAttribute(params string[] policies)
    {
        Policy = OpaAuthorizationDefaults.PolicyName;
        Policies = policies;
    }

}
